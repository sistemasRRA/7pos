using Microsoft.EntityFrameworkCore;
using SevenPos.Core.Data;

namespace SevenPos.Core.Organization;

public enum LoginFailure { UserNotFound, UserInactive, UserLocked, WrongPin }

public record LoginResult(User? User, LoginFailure? Failure, int RemainingAttempts = 0, DateTime? LockedUntilUtc = null)
{
    public bool Succeeded => User is not null;
}

/// <summary>PIN login for the POS. Rules: docs/features/pin-login.md.</summary>
public class LoginService(IDbContextFactory<PosDbContext> dbFactory, TimeProvider clock)
{
    public const int MaxFailedAttempts = 3;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The branch this terminal works for. Temporary rule until device registration
    /// (DISPOSITIVOS in ER rev. 2) exists: the first active branch by name.
    /// </summary>
    public async Task<Branch?> GetTerminalBranchAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).AsNoTracking().FirstOrDefaultAsync();
    }

    /// <summary>Active users of a branch, for the "tap your name" screen.</summary>
    public async Task<List<User>> GetUsersAsync(Guid branchId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Users
            .Where(u => u.BranchId == branchId && u.IsActive)
            .OrderBy(u => u.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<LoginResult> LoginAsync(Guid userId, string pin)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId);

        if (user is null) return new(null, LoginFailure.UserNotFound);
        if (!user.IsActive) return new(null, LoginFailure.UserInactive);

        var now = clock.GetUtcNow().UtcDateTime;
        if (user.LockedUntilUtc > now) return new(null, LoginFailure.UserLocked, LockedUntilUtc: user.LockedUntilUtc);

        if (!PinHasher.Verify(pin, user.PinHash))
        {
            user.FailedPinAttempts++;
            Audit(db, user, "LOGIN_FAILED", now);

            if (user.FailedPinAttempts >= MaxFailedAttempts)
            {
                // Start counting from zero again once the lock expires.
                user.FailedPinAttempts = 0;
                user.LockedUntilUtc = now + LockoutDuration;
                Audit(db, user, "USER_LOCKED", now);
                await db.SaveChangesAsync();
                return new(null, LoginFailure.UserLocked, LockedUntilUtc: user.LockedUntilUtc);
            }

            await db.SaveChangesAsync();
            return new(null, LoginFailure.WrongPin, RemainingAttempts: MaxFailedAttempts - user.FailedPinAttempts);
        }

        user.FailedPinAttempts = 0;
        user.LockedUntilUtc = null;
        Audit(db, user, "LOGIN", now);
        await db.SaveChangesAsync();
        return new(user, null);
    }

    private static void Audit(PosDbContext db, User user, string action, DateTime nowUtc) =>
        db.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id, BranchId = user.BranchId, Action = action, CreatedAtUtc = nowUtc,
        });
}
