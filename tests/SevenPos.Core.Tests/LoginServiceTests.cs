using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SevenPos.Core.Organization;

namespace SevenPos.Core.Tests;

/// <summary>One test per rule in docs/features/pin-login.md.</summary>
public class LoginServiceTests : IDisposable
{
    private const string Pin = "1234";
    private readonly TestDatabase _database = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 26, 15, 0, 0, TimeSpan.Zero));
    private readonly LoginService _login;
    private readonly Branch _branch = new() { Name = "Sucursal Prueba", Code = "PRB" };
    private readonly User _cashier;

    public LoginServiceTests()
    {
        _login = new LoginService(_database, _clock);
        _cashier = new User { Name = "Ana", PinHash = PinHasher.Hash(Pin), BranchId = _branch.Id };
        using var db = _database.CreateDbContext();
        db.Branches.Add(_branch);
        db.Users.Add(_cashier);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    private async Task<User> ReloadCashierAsync()
    {
        await using var db = _database.CreateDbContext();
        return await db.Users.SingleAsync(u => u.Id == _cashier.Id);
    }

    [Fact]
    public async Task CorrectPin_LogsIn()
    {
        var result = await _login.LoginAsync(_cashier.Id, Pin);

        Assert.True(result.Succeeded);
        Assert.Equal("Ana", result.User!.Name);
    }

    [Fact]
    public async Task WrongPin_IsRejected_AndSaysHowManyAttemptsAreLeft()
    {
        var result = await _login.LoginAsync(_cashier.Id, "9999");

        Assert.False(result.Succeeded);
        Assert.Equal(LoginFailure.WrongPin, result.Failure);
        Assert.Equal(2, result.RemainingAttempts);
    }

    [Fact]
    public async Task ThirdWrongPin_LocksUser_EvenForTheCorrectPin()
    {
        await _login.LoginAsync(_cashier.Id, "9999");
        await _login.LoginAsync(_cashier.Id, "9999");
        var third = await _login.LoginAsync(_cashier.Id, "9999");

        Assert.Equal(LoginFailure.UserLocked, third.Failure);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime + LoginService.LockoutDuration, third.LockedUntilUtc);

        var withCorrectPin = await _login.LoginAsync(_cashier.Id, Pin);
        Assert.Equal(LoginFailure.UserLocked, withCorrectPin.Failure);
    }

    [Fact]
    public async Task Lock_ExpiresAfterFiveMinutes()
    {
        for (var i = 0; i < LoginService.MaxFailedAttempts; i++)
            await _login.LoginAsync(_cashier.Id, "9999");

        _clock.Advance(TimeSpan.FromMinutes(4));
        Assert.Equal(LoginFailure.UserLocked, (await _login.LoginAsync(_cashier.Id, Pin)).Failure);

        _clock.Advance(TimeSpan.FromMinutes(1));
        Assert.True((await _login.LoginAsync(_cashier.Id, Pin)).Succeeded);
    }

    [Fact]
    public async Task SuccessfulLogin_ResetsFailedAttempts()
    {
        await _login.LoginAsync(_cashier.Id, "9999");
        await _login.LoginAsync(_cashier.Id, "9999");
        await _login.LoginAsync(_cashier.Id, Pin);

        var user = await ReloadCashierAsync();
        Assert.Equal(0, user.FailedPinAttempts);
        Assert.Null(user.LockedUntilUtc);
    }

    [Fact]
    public async Task InactiveUser_CannotLogIn()
    {
        await using (var db = _database.CreateDbContext())
        {
            var user = await db.Users.SingleAsync(u => u.Id == _cashier.Id);
            user.IsActive = false;
            await db.SaveChangesAsync();
        }

        var result = await _login.LoginAsync(_cashier.Id, Pin);

        Assert.Equal(LoginFailure.UserInactive, result.Failure);
    }

    [Fact]
    public async Task UnknownUser_IsRejected()
    {
        var result = await _login.LoginAsync(Guid.NewGuid(), Pin);

        Assert.Equal(LoginFailure.UserNotFound, result.Failure);
    }

    [Fact]
    public async Task LoginsAndFailures_AreWrittenToTheAuditLog()
    {
        await _login.LoginAsync(_cashier.Id, "9999");
        _clock.Advance(TimeSpan.FromSeconds(1));
        await _login.LoginAsync(_cashier.Id, Pin);

        await using var db = _database.CreateDbContext();
        var actions = await db.AuditLogs
            .Where(a => a.UserId == _cashier.Id && a.BranchId == _branch.Id)
            .OrderBy(a => a.CreatedAtUtc)
            .Select(a => a.Action)
            .ToListAsync();
        Assert.Equal(["LOGIN_FAILED", "LOGIN"], actions);
    }

    [Fact]
    public async Task TerminalBranch_IsTheFirstActiveBranchByName()
    {
        await using (var db = _database.CreateDbContext())
        {
            db.Branches.AddRange(
                new Branch { Name = "Aaa cerrada", Code = "AAA", IsActive = false },
                new Branch { Name = "Zzz", Code = "ZZZ" });
            await db.SaveChangesAsync();
        }

        var branch = await _login.GetTerminalBranchAsync();

        Assert.Equal("Sucursal Prueba", branch!.Name);
    }

    [Fact]
    public async Task GetUsers_ReturnsOnlyActiveUsersOfTheBranch_SortedByName()
    {
        await using (var db = _database.CreateDbContext())
        {
            var otherBranch = new Branch { Name = "Otra", Code = "OTRA" };
            db.Branches.Add(otherBranch);
            db.Users.AddRange(
                new User { Name = "Beto", PinHash = PinHasher.Hash("1111"), BranchId = _branch.Id },
                new User { Name = "Carla", PinHash = PinHasher.Hash("2222"), BranchId = _branch.Id, IsActive = false },
                new User { Name = "Diego", PinHash = PinHasher.Hash("3333"), BranchId = otherBranch.Id });
            await db.SaveChangesAsync();
        }

        var users = await _login.GetUsersAsync(_branch.Id);

        Assert.Equal(["Ana", "Beto"], users.Select(u => u.Name));
    }
}
