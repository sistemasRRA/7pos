using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SevenPos.Core.Cash;
using SevenPos.Core.Guests;
using SevenPos.Core.Organization;
using SevenPos.Core.Tickets;

namespace SevenPos.Core.Tests;

/// <summary>One test per rule in docs/features/shifts.md.</summary>
public class ShiftServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 27, 15, 0, 0, TimeSpan.Zero));
    private readonly ShiftService _shifts;

    private readonly Branch _branch = new() { Name = "Centro", Code = "CEN" };
    private readonly Branch _otherBranch = new() { Name = "Norte", Code = "NTE" };
    private readonly User _cashier, _secondCashier, _otherBranchCashier, _marketing, _inactive;

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    public ShiftServiceTests()
    {
        _shifts = new ShiftService(_database, _clock);
        _cashier = new User { Name = "Carla", PinHash = "x", Role = UserRole.Cashier, BranchId = _branch.Id };
        _secondCashier = new User { Name = "Beto", PinHash = "x", Role = UserRole.Cashier, BranchId = _branch.Id };
        _otherBranchCashier = new User { Name = "Nora", PinHash = "x", Role = UserRole.Cashier, BranchId = _otherBranch.Id };
        _marketing = new User { Name = "Mónica", PinHash = "x", Role = UserRole.Marketing, BranchId = _branch.Id };
        _inactive = new User { Name = "Ida", PinHash = "x", Role = UserRole.Cashier, BranchId = _branch.Id, IsActive = false };

        using var db = _database.CreateDbContext();
        db.Branches.AddRange(_branch, _otherBranch);
        db.Users.AddRange(_cashier, _secondCashier, _otherBranchCashier, _marketing, _inactive);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    private async Task<Shift> OpenAsync(decimal openingCash = 500)
    {
        var result = await _shifts.OpenAsync(_cashier.Id, openingCash);
        Assert.Null(result.Error);
        return result.Shift!;
    }

    /// <summary>Inserts a payment directly (payments are the next feature) to set up a scenario.</summary>
    private void AddPayment(Shift shift, decimal amount, PaymentMethod method)
    {
        using var db = _database.CreateDbContext();
        var guest = new Guest { Phone = $"55{Random.Shared.Next(10_000_000, 99_999_999)}", FullName = "Cliente" };
        var ticket = new Ticket
        {
            Folio = Random.Shared.Next(1, 1_000_000), PublicOrderId = Guid.NewGuid().ToString("N")[..8],
            BranchId = _branch.Id, GuestId = guest.Id, CashierId = _cashier.Id, ShiftId = shift.Id,
        };
        db.Guests.Add(guest);
        db.Tickets.Add(ticket);
        db.Payments.Add(new Payment { TicketId = ticket.Id, ShiftId = shift.Id, Amount = amount, Method = method, Type = PaymentType.Advance });
        db.SaveChanges();
    }

    private async Task<List<AuditLog>> AuditAsync(string action)
    {
        await using var db = _database.CreateDbContext();
        return await db.AuditLogs.Where(a => a.Action == action).ToListAsync();
    }

    // ---------- Opening ----------

    [Fact]
    public async Task Open_StartsAShiftOnTheUsersBranch_WithTheOpeningCash()
    {
        var result = await _shifts.OpenAsync(_cashier.Id, 500);

        Assert.True(result.Succeeded);
        var open = await _shifts.GetOpenShiftAsync(_branch.Id);
        Assert.NotNull(open);
        Assert.Equal(_cashier.Id, open.UserId);
        Assert.Equal(500m, open.OpeningCash);
        Assert.Equal(Now, open.OpenedAtUtc);
        Assert.Null(await _shifts.GetOpenShiftAsync(_otherBranch.Id));
    }

    [Fact]
    public async Task Open_IsAudited()
    {
        await OpenAsync(500);

        var audit = Assert.Single(await AuditAsync("SHIFT_OPENED"));
        Assert.Equal(_cashier.Id, audit.UserId);
        Assert.Equal(_branch.Id, audit.BranchId);
        Assert.Contains("500", audit.NewValue);
    }

    [Fact]
    public async Task Open_WithZeroOpeningCash_IsAllowed()
    {
        Assert.True((await _shifts.OpenAsync(_cashier.Id, 0)).Succeeded);
    }

    [Fact]
    public async Task Open_WithNegativeOpeningCash_IsRefused()
    {
        Assert.Equal(ShiftError.InvalidAmount, (await _shifts.OpenAsync(_cashier.Id, -1)).Error);
    }

    [Fact]
    public async Task Open_WhenTheBranchAlreadyHasAnOpenShift_IsRefused()
    {
        await OpenAsync();

        var second = await _shifts.OpenAsync(_secondCashier.Id, 200);

        Assert.Equal(ShiftError.ShiftAlreadyOpen, second.Error);
        Assert.True((await _shifts.OpenAsync(_otherBranchCashier.Id, 200)).Succeeded); // other branches are independent
    }

    [Fact]
    public async Task Open_TwoAtTheSameTime_OnlyOneWins()
    {
        var results = await Task.WhenAll(
            _shifts.OpenAsync(_cashier.Id, 100),
            _shifts.OpenAsync(_secondCashier.Id, 200));

        Assert.Single(results, r => r.Succeeded);
        Assert.Single(results, r => r.Error == ShiftError.ShiftAlreadyOpen);
    }

    [Theory]
    [InlineData("marketing")]
    [InlineData("inactive")]
    [InlineData("unknown")]
    public async Task Open_ByMarketingOrAnInactiveOrUnknownUser_IsRefused(string who)
    {
        var userId = who switch { "marketing" => _marketing.Id, "inactive" => _inactive.Id, _ => Guid.NewGuid() };

        Assert.Equal(ShiftError.NotAllowed, (await _shifts.OpenAsync(userId, 100)).Error);
    }

    // ---------- Withdrawals ----------

    [Fact]
    public async Task Withdraw_RecordsAmountReasonAndWhoTookIt_OnTheOpenShift()
    {
        var shift = await OpenAsync(500);

        var result = await _shifts.WithdrawAsync(_secondCashier.Id, 120, "  Pago a proveedor de ganchos  ");

        Assert.True(result.Succeeded);
        var open = await _shifts.GetOpenShiftAsync(_branch.Id);
        var withdrawal = Assert.Single(open!.Withdrawals);
        Assert.Equal(shift.Id, withdrawal.ShiftId);
        Assert.Equal(_secondCashier.Id, withdrawal.UserId);
        Assert.Equal(120m, withdrawal.Amount);
        Assert.Equal("Pago a proveedor de ganchos", withdrawal.Reason);
        Assert.Equal(Now, withdrawal.CreatedAtUtc);
        Assert.Single(await AuditAsync("CASH_WITHDRAWN"));
    }

    [Fact]
    public async Task Withdraw_WithoutAnOpenShift_IsRefused()
    {
        Assert.Equal(ShiftError.NoOpenShift, (await _shifts.WithdrawAsync(_cashier.Id, 50, "Gasolina")).Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Withdraw_ZeroOrNegative_IsRefused(decimal amount)
    {
        await OpenAsync();

        Assert.Equal(ShiftError.InvalidAmount, (await _shifts.WithdrawAsync(_cashier.Id, amount, "Gasolina")).Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Withdraw_WithoutAReason_IsRefused(string reason)
    {
        await OpenAsync();

        Assert.Equal(ShiftError.ReasonRequired, (await _shifts.WithdrawAsync(_cashier.Id, 50, reason)).Error);
    }

    [Fact]
    public async Task Withdraw_MoreThanTheCashInTheDrawer_IsRefused()
    {
        var shift = await OpenAsync(100);
        AddPayment(shift, 50, PaymentMethod.Cash);
        AddPayment(shift, 500, PaymentMethod.Card); // card money is not in the drawer
        Assert.True((await _shifts.WithdrawAsync(_cashier.Id, 30, "Gasolina")).Succeeded);

        Assert.Equal(ShiftError.NotEnoughCash, (await _shifts.WithdrawAsync(_cashier.Id, 121, "Proveedor")).Error);
        Assert.True((await _shifts.WithdrawAsync(_cashier.Id, 120, "Proveedor")).Succeeded); // exactly what is left
    }

    [Fact]
    public async Task Withdraw_TwoAtTheSameTime_CannotTakeMoreThanTheDrawerHas()
    {
        await OpenAsync(100);

        var results = await Task.WhenAll(
            _shifts.WithdrawAsync(_cashier.Id, 60, "Proveedor"),
            _shifts.WithdrawAsync(_secondCashier.Id, 60, "Gasolina"));

        Assert.Single(results, r => r.Succeeded);
        Assert.Single(results, r => r.Error == ShiftError.NotEnoughCash);
    }

    [Fact]
    public async Task Withdraw_ByMarketing_IsRefused()
    {
        await OpenAsync();

        Assert.Equal(ShiftError.NotAllowed, (await _shifts.WithdrawAsync(_marketing.Id, 50, "Gasolina")).Error);
    }

    // ---------- Close (Corte Z) ----------

    [Fact]
    public async Task Summary_ExpectedCash_IsOpeningPlusCashPaymentsMinusWithdrawals()
    {
        var shift = await OpenAsync(500);
        AddPayment(shift, 200, PaymentMethod.Cash);
        AddPayment(shift, 85.50m, PaymentMethod.Cash);
        AddPayment(shift, 300, PaymentMethod.Card);
        await _shifts.WithdrawAsync(_cashier.Id, 100, "Proveedor");

        var summary = await _shifts.GetSummaryAsync(shift.Id);

        Assert.NotNull(summary);
        Assert.Equal(285.50m, summary.CashPayments);
        Assert.Equal(100m, summary.WithdrawalsTotal);
        Assert.Equal(685.50m, summary.ExpectedCash); // 500 + 285.50 − 100
        Assert.Equal(285.50m, summary.PaymentsByMethod[PaymentMethod.Cash]);
        Assert.Equal(300m, summary.PaymentsByMethod[PaymentMethod.Card]);
        Assert.Equal("Carla", summary.OpenedBy);
        Assert.Equal("Proveedor", Assert.Single(summary.Withdrawals).Reason);
    }

    [Theory]
    [InlineData(650, 0)]     // exact
    [InlineData(640, -10)]   // short
    [InlineData(655.5, 5.5)] // over
    public async Task Close_StoresTheBlindCountAndTheDifference(decimal counted, decimal difference)
    {
        var shift = await OpenAsync(500);
        AddPayment(shift, 150, PaymentMethod.Cash);
        _clock.Advance(TimeSpan.FromHours(8));

        var result = await _shifts.CloseAsync(_secondCashier.Id, counted);

        Assert.True(result.Succeeded);
        var closed = result.Summary!.Shift;
        Assert.Equal(Now, closed.ClosedAtUtc);
        Assert.Equal(_secondCashier.Id, closed.ClosedByUserId);
        Assert.Equal(counted, closed.BlindCashCount);
        Assert.Equal(difference, closed.CashDifference);
        Assert.Equal(650m, result.Summary.ExpectedCash);
        Assert.Equal("Beto", result.Summary.ClosedBy);
        Assert.Null(await _shifts.GetOpenShiftAsync(_branch.Id));
    }

    [Fact]
    public async Task Close_IsAudited_WithExpectedCountedAndDifference()
    {
        await OpenAsync(500);

        await _shifts.CloseAsync(_cashier.Id, 490);

        var audit = Assert.Single(await AuditAsync("SHIFT_CLOSED"));
        Assert.Contains("\"expected\":500", audit.NewValue);
        Assert.Contains("\"counted\":490", audit.NewValue);
        Assert.Contains("\"difference\":-10", audit.NewValue);
    }

    [Fact]
    public async Task Close_WithoutAnOpenShift_IsRefused()
    {
        Assert.Equal(ShiftError.NoOpenShift, (await _shifts.CloseAsync(_cashier.Id, 0)).Error);
    }

    [Fact]
    public async Task Close_WithANegativeCount_IsRefused()
    {
        await OpenAsync();

        Assert.Equal(ShiftError.InvalidAmount, (await _shifts.CloseAsync(_cashier.Id, -1)).Error);
    }

    [Fact]
    public async Task Close_ByMarketing_IsRefused()
    {
        await OpenAsync();

        Assert.Equal(ShiftError.NotAllowed, (await _shifts.CloseAsync(_marketing.Id, 500)).Error);
    }

    [Fact]
    public async Task AfterClosing_ANewShiftCanBeOpened_AndWithdrawalsNeedIt()
    {
        await OpenAsync(500);
        await _shifts.CloseAsync(_cashier.Id, 500);

        Assert.Equal(ShiftError.NoOpenShift, (await _shifts.WithdrawAsync(_cashier.Id, 10, "Gasolina")).Error);
        Assert.True((await _shifts.OpenAsync(_secondCashier.Id, 300)).Succeeded);
    }
}
