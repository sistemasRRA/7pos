using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SevenPos.Core.Data;

namespace SevenPos.Core.Cash;

public enum ShiftError { NotAllowed, InvalidAmount, ShiftAlreadyOpen, NoOpenShift, ReasonRequired, NotEnoughCash }

public record ShiftResult(Shift? Shift, ShiftError? Error)
{
    public bool Succeeded => Shift is not null;
}

public record WithdrawalResult(CashWithdrawal? Withdrawal, ShiftError? Error)
{
    public bool Succeeded => Withdrawal is not null;
}

public record WithdrawalLine(decimal Amount, string Reason, string TakenBy, DateTime CreatedAtUtc);

/// <summary>
/// The numbers of a shift, always computed from its rows (ADR 0007).
/// ExpectedCash = opening cash + cash payments − withdrawals.
/// </summary>
public record ShiftSummary(
    Shift Shift,
    string OpenedBy,
    string? ClosedBy,
    IReadOnlyList<WithdrawalLine> Withdrawals,
    IReadOnlyDictionary<PaymentMethod, decimal> PaymentsByMethod,
    decimal CashPayments,
    decimal WithdrawalsTotal,
    decimal ExpectedCash);

public record ShiftCloseResult(ShiftSummary? Summary, ShiftError? Error)
{
    public bool Succeeded => Summary is not null;
}

/// <summary>Opening, cash withdrawals and closing (Corte Z) of a branch's cash drawer. Rules: docs/features/shifts.md.</summary>
public class ShiftService(IDbContextFactory<PosDbContext> dbFactory, TimeProvider clock)
{
    /// <summary>The branch's open shift with its withdrawals (oldest first), or null.</summary>
    public async Task<Shift?> GetOpenShiftAsync(Guid branchId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Shifts
            .Include(s => s.Withdrawals.OrderBy(w => w.CreatedAtUtc))
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.BranchId == branchId && s.ClosedAtUtc == null);
    }

    /// <summary>
    /// Opens a shift on the user's branch. A branch has one cash drawer, so it can have
    /// only one open shift; the database enforces it (ix_shifts_open_per_branch).
    /// </summary>
    public async Task<ShiftResult> OpenAsync(Guid actorUserId, decimal openingCash)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var actor = await AllowedUserAsync(db, actorUserId);
        if (actor is null) return new(null, ShiftError.NotAllowed);
        if (openingCash < 0) return new(null, ShiftError.InvalidAmount);
        if (await db.Shifts.AnyAsync(s => s.BranchId == actor.BranchId && s.ClosedAtUtc == null))
            return new(null, ShiftError.ShiftAlreadyOpen);

        var now = clock.GetUtcNow().UtcDateTime;
        var shift = new Shift { BranchId = actor.BranchId, UserId = actor.Id, OpeningCash = openingCash, OpenedAtUtc = now };
        db.Shifts.Add(shift);
        db.AuditLogs.Add(Audit(actor, "SHIFT_OPENED", new { openingCash }, now));
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another terminal opened the drawer between our check and our insert.
            return new(null, ShiftError.ShiftAlreadyOpen);
        }
        return new(shift, null);
    }

    /// <summary>
    /// Takes cash out of the drawer of the user's branch (supplier, expenses, pickups).
    /// It can never be more than the cash the drawer should have.
    /// </summary>
    public async Task<WithdrawalResult> WithdrawAsync(Guid actorUserId, decimal amount, string reason)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var actor = await AllowedUserAsync(db, actorUserId);
        if (actor is null) return new(null, ShiftError.NotAllowed);
        if (amount <= 0) return new(null, ShiftError.InvalidAmount);
        reason = reason.Trim();
        if (reason.Length == 0) return new(null, ShiftError.ReasonRequired);

        await using var transaction = await db.Database.BeginTransactionAsync();
        var shift = await LockOpenShiftAsync(db, actor.BranchId);
        if (shift is null) return new(null, ShiftError.NoOpenShift);
        if (amount > await ExpectedCashAsync(db, shift)) return new(null, ShiftError.NotEnoughCash);

        var now = clock.GetUtcNow().UtcDateTime;
        var withdrawal = new CashWithdrawal { ShiftId = shift.Id, UserId = actor.Id, Amount = amount, Reason = reason, CreatedAtUtc = now };
        db.CashWithdrawals.Add(withdrawal);
        db.AuditLogs.Add(Audit(actor, "CASH_WITHDRAWN", new { shiftId = shift.Id, amount, reason }, now));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new(withdrawal, null);
    }

    /// <summary>
    /// Closes the branch's open shift (Corte Z) with the cash the user counted without
    /// seeing the expected amount (blind count). Stores the count and the difference.
    /// </summary>
    public async Task<ShiftCloseResult> CloseAsync(Guid actorUserId, decimal countedCash)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var actor = await AllowedUserAsync(db, actorUserId);
        if (actor is null) return new(null, ShiftError.NotAllowed);
        if (countedCash < 0) return new(null, ShiftError.InvalidAmount);

        await using var transaction = await db.Database.BeginTransactionAsync();
        var shift = await LockOpenShiftAsync(db, actor.BranchId);
        if (shift is null) return new(null, ShiftError.NoOpenShift);

        var expected = await ExpectedCashAsync(db, shift);
        var now = clock.GetUtcNow().UtcDateTime;
        shift.ClosedAtUtc = now;
        shift.ClosedByUserId = actor.Id;
        shift.BlindCashCount = countedCash;
        shift.CashDifference = countedCash - expected;
        db.AuditLogs.Add(Audit(actor, "SHIFT_CLOSED",
            new { shiftId = shift.Id, expected, counted = countedCash, difference = shift.CashDifference }, now));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new(await GetSummaryAsync(shift.Id), null);
    }

    public async Task<ShiftSummary?> GetSummaryAsync(Guid shiftId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var shift = await db.Shifts.AsNoTracking().SingleOrDefaultAsync(s => s.Id == shiftId);
        if (shift is null) return null;

        var withdrawals = await db.CashWithdrawals
            .Where(w => w.ShiftId == shiftId)
            .OrderBy(w => w.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync();
        var userIds = withdrawals.Select(w => w.UserId).Append(shift.UserId).Append(shift.ClosedByUserId ?? shift.UserId).Distinct().ToList();
        var names = await db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name);
        var byMethod = await db.Payments
            .Where(p => p.ShiftId == shiftId && p.IsActive)
            .GroupBy(p => p.Method)
            .Select(g => new { Method = g.Key, Total = g.Sum(p => p.Amount) })
            .ToDictionaryAsync(x => x.Method, x => x.Total);

        var cash = byMethod.GetValueOrDefault(PaymentMethod.Cash);
        var withdrawn = withdrawals.Sum(w => w.Amount);
        return new ShiftSummary(
            shift,
            names[shift.UserId],
            shift.ClosedByUserId is { } closedBy ? names[closedBy] : null,
            withdrawals.Select(w => new WithdrawalLine(w.Amount, w.Reason, names[w.UserId], w.CreatedAtUtc)).ToList(),
            byMethod,
            cash,
            withdrawn,
            shift.OpeningCash + cash - withdrawn);
    }

    /// <summary>Branch staff only: Marketing does not run the cash drawer. The role is read from the database.</summary>
    private static async Task<User?> AllowedUserAsync(PosDbContext db, Guid userId)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);
        return user is { IsActive: true } && user.Role != UserRole.Marketing ? user : null;
    }

    /// <summary>
    /// Reads the branch's open shift and locks its row until the transaction ends
    /// (SELECT … FOR UPDATE). Two withdrawals, or a withdrawal and the close, on the same
    /// drawer then run one after the other, so the cash check always sees the latest rows.
    /// </summary>
    private static Task<Shift?> LockOpenShiftAsync(PosDbContext db, Guid branchId) =>
        db.Shifts
            .FromSql($"SELECT * FROM shifts WHERE branch_id = {branchId} AND closed_at_utc IS NULL FOR UPDATE")
            .SingleOrDefaultAsync();

    private static async Task<decimal> ExpectedCashAsync(PosDbContext db, Shift shift)
    {
        var cash = await db.Payments
            .Where(p => p.ShiftId == shift.Id && p.IsActive && p.Method == PaymentMethod.Cash)
            .SumAsync(p => p.Amount);
        var withdrawn = await db.CashWithdrawals.Where(w => w.ShiftId == shift.Id).SumAsync(w => w.Amount);
        return shift.OpeningCash + cash - withdrawn;
    }

    private static AuditLog Audit(User actor, string action, object newValue, DateTime now) => new()
    {
        UserId = actor.Id,
        BranchId = actor.BranchId,
        Action = action,
        NewValue = JsonSerializer.Serialize(newValue),
        CreatedAtUtc = now,
    };
}
