using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SevenPos.Core.Data;

namespace SevenPos.Core.Cash;

public enum PaymentError { NotAllowed, TicketNotFound, OtherBranch, TicketCancelled, NoOpenShift, InvalidAmount, AmountExceedsBalance, MethodNotAccepted }

public record PaymentResult(Payment? Payment, decimal BalanceDue, PaymentError? Error)
{
    public bool Succeeded => Payment is not null;
}

/// <summary>Money received for a ticket. Rules: docs/features/payments.md.</summary>
public class PaymentService(IDbContextFactory<PosDbContext> dbFactory, TimeProvider clock)
{
    /// <summary>Wallet and payment links arrive in Phase 3.</summary>
    public static readonly IReadOnlyList<PaymentMethod> AcceptedMethods = [PaymentMethod.Cash, PaymentMethod.Card];

    /// <summary>More than $0, whole cents.</summary>
    public static bool IsValidAmount(decimal amount) => amount > 0 && decimal.Round(amount, 2) == amount;

    /// <summary>
    /// Pays part or all of a ticket's balance after it was created (usually at pickup):
    /// a Settlement on the branch's open shift.
    /// </summary>
    public async Task<PaymentResult> PayAsync(Guid actorUserId, Guid ticketId, decimal amount, PaymentMethod method)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var actor = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == actorUserId);
        if (actor is not { IsActive: true } || actor.Role == UserRole.Marketing) return Fail(PaymentError.NotAllowed);
        if (!IsValidAmount(amount)) return Fail(PaymentError.InvalidAmount);
        if (!AcceptedMethods.Contains(method)) return Fail(PaymentError.MethodNotAccepted);

        await using var transaction = await db.Database.BeginTransactionAsync();
        // Lock the ticket: two terminals paying the same ticket run one after the other,
        // so the second one sees the balance the first one left.
        var ticket = await db.Tickets
            .FromSql($"SELECT * FROM tickets WHERE id = {ticketId} FOR UPDATE")
            .SingleOrDefaultAsync();
        if (ticket is null) return Fail(PaymentError.TicketNotFound);
        if (ticket.BranchId != actor.BranchId) return Fail(PaymentError.OtherBranch);
        if (ticket.Status == TicketStatus.Cancelled) return Fail(PaymentError.TicketCancelled);
        if (amount > ticket.BalanceDue) return Fail(PaymentError.AmountExceedsBalance, ticket.BalanceDue);

        var shift = await LockOpenShiftAsync(db, actor.BranchId);
        if (shift is null) return Fail(PaymentError.NoOpenShift, ticket.BalanceDue);

        var payment = Record(db, ticket, shift.Id, actor, amount, method, PaymentType.Settlement, clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new(payment, ticket.BalanceDue, null);
    }

    /// <summary>
    /// The branch's open shift, locked for sharing until the transaction ends (SELECT … FOR SHARE).
    /// Many payments can hold it at once, but the close (FOR UPDATE) waits for them, and a
    /// payment that waited for a close finds no open shift. So no payment lands on a closed shift.
    /// </summary>
    internal static Task<Shift?> LockOpenShiftAsync(PosDbContext db, Guid branchId) =>
        db.Shifts
            .FromSql($"SELECT * FROM shifts WHERE branch_id = {branchId} AND closed_at_utc IS NULL FOR SHARE")
            .SingleOrDefaultAsync();

    /// <summary>
    /// Adds the payment, lowers the ticket's balance and writes PAYMENT_RECEIVED. The caller
    /// has validated it and saves it in the same transaction as the ticket.
    /// </summary>
    internal static Payment Record(PosDbContext db, Ticket ticket, Guid shiftId, User actor,
        decimal amount, PaymentMethod method, PaymentType type, DateTime now)
    {
        var payment = new Payment
        {
            TicketId = ticket.Id, ShiftId = shiftId, UserId = actor.Id,
            Amount = amount, Method = method, Type = type, PaidAtUtc = now,
        };
        db.Payments.Add(payment);
        ticket.BalanceDue -= amount;
        db.AuditLogs.Add(new AuditLog
        {
            UserId = actor.Id,
            BranchId = actor.BranchId,
            Action = "PAYMENT_RECEIVED",
            NewValue = JsonSerializer.Serialize(new
            {
                ticketId = ticket.Id, folio = ticket.Folio, amount, method = method.ToString(), type = type.ToString(),
                balanceDue = ticket.BalanceDue,
            }),
            CreatedAtUtc = now,
        });
        return payment;
    }

    private static PaymentResult Fail(PaymentError error, decimal balanceDue = 0) => new(null, balanceDue, error);
}
