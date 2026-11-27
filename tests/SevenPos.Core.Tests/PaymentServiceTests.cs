using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SevenPos.Core.Cash;
using SevenPos.Core.Guests;
using SevenPos.Core.Organization;
using SevenPos.Core.Tickets;

namespace SevenPos.Core.Tests;

/// <summary>One test per rule in docs/features/payments.md (payments after the ticket is created).</summary>
public class PaymentServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 27, 18, 0, 0, TimeSpan.Zero));
    private readonly PaymentService _payments;
    private readonly ShiftService _shifts;

    private readonly Branch _branch = new() { Name = "Centro", Code = "CEN" };
    private readonly Branch _otherBranch = new() { Name = "Norte", Code = "NTE" };
    private readonly User _cashier, _secondCashier, _otherBranchCashier, _marketing, _inactive;
    private readonly Guest _guest = new() { Phone = "5512345678", FullName = "Ana López" };

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    public PaymentServiceTests()
    {
        _payments = new PaymentService(_database, _clock);
        _shifts = new ShiftService(_database, _clock);
        _cashier = NewUser("Carla", UserRole.Cashier, _branch);
        _secondCashier = NewUser("Beto", UserRole.Cashier, _branch);
        _otherBranchCashier = NewUser("Nora", UserRole.Cashier, _otherBranch);
        _marketing = NewUser("Mónica", UserRole.Marketing, _branch);
        _inactive = NewUser("Ida", UserRole.Cashier, _branch);
        _inactive.IsActive = false;

        using var db = _database.CreateDbContext();
        db.Branches.AddRange(_branch, _otherBranch);
        db.Users.AddRange(_cashier, _secondCashier, _otherBranchCashier, _marketing, _inactive);
        db.Guests.Add(_guest);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    private static User NewUser(string name, UserRole role, Branch branch) =>
        new() { Name = name, PinHash = "x", Role = role, BranchId = branch.Id };

    /// <summary>Inserts a ticket directly (its garments do not matter here) owing the given balance.</summary>
    private Ticket AddTicket(decimal total, decimal? balanceDue = null, Branch? branch = null, TicketStatus status = TicketStatus.Received)
    {
        using var db = _database.CreateDbContext();
        var folio = db.Tickets.Count() + 1;
        var ticket = new Ticket
        {
            Folio = folio, PublicOrderId = $"ORD{folio:D5}", BranchId = (branch ?? _branch).Id, GuestId = _guest.Id,
            CashierId = _cashier.Id, Subtotal = total, Total = total, BalanceDue = balanceDue ?? total, Status = status,
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket;
    }

    private async Task<Shift> OpenShiftAsync(User? user = null, decimal openingCash = 500) =>
        (await _shifts.OpenAsync((user ?? _cashier).Id, openingCash)).Shift!;

    private async Task<Ticket> ReloadAsync(Ticket ticket)
    {
        await using var db = _database.CreateDbContext();
        return await db.Tickets.Include(t => t.Payments).SingleAsync(t => t.Id == ticket.Id);
    }

    // ---------- Paying a balance ----------

    [Fact]
    public async Task Pay_RecordsASettlement_OnTheOpenShift_AndLowersTheBalance()
    {
        var shift = await OpenShiftAsync();
        var ticket = AddTicket(total: 300, balanceDue: 200);

        var result = await _payments.PayAsync(_secondCashier.Id, ticket.Id, 150, PaymentMethod.Card);

        Assert.True(result.Succeeded);
        Assert.Equal(50m, result.BalanceDue);
        var saved = await ReloadAsync(ticket);
        Assert.Equal(50m, saved.BalanceDue);
        var payment = Assert.Single(saved.Payments);
        Assert.Equal(150m, payment.Amount);
        Assert.Equal(PaymentMethod.Card, payment.Method);
        Assert.Equal(PaymentType.Settlement, payment.Type);
        Assert.Equal(shift.Id, payment.ShiftId);
        Assert.Equal(_secondCashier.Id, payment.UserId);
        Assert.Equal(Now, payment.PaidAtUtc);
    }

    [Fact]
    public async Task Pay_TheWholeBalance_LeavesNothingToPay()
    {
        await OpenShiftAsync();
        var ticket = AddTicket(total: 255.50m);

        var result = await _payments.PayAsync(_cashier.Id, ticket.Id, 255.50m, PaymentMethod.Cash);

        Assert.Equal(0m, result.BalanceDue);
        Assert.Equal(0m, (await ReloadAsync(ticket)).BalanceDue);
    }

    [Fact]
    public async Task Pay_InCash_CountsInTheShiftsExpectedCash()
    {
        var shift = await OpenShiftAsync(openingCash: 500);
        var ticket = AddTicket(total: 300);

        await _payments.PayAsync(_cashier.Id, ticket.Id, 120, PaymentMethod.Cash);
        await _payments.PayAsync(_cashier.Id, ticket.Id, 80, PaymentMethod.Card);

        var summary = await _shifts.GetSummaryAsync(shift.Id);
        Assert.Equal(620m, summary!.ExpectedCash);
        Assert.Equal(80m, summary.PaymentsByMethod[PaymentMethod.Card]);
    }

    [Fact]
    public async Task Pay_IsAudited()
    {
        await OpenShiftAsync();
        var ticket = AddTicket(total: 300);

        await _payments.PayAsync(_cashier.Id, ticket.Id, 100, PaymentMethod.Cash);

        await using var db = _database.CreateDbContext();
        var audit = await db.AuditLogs.SingleAsync(a => a.Action == "PAYMENT_RECEIVED");
        Assert.Equal(_cashier.Id, audit.UserId);
        Assert.Contains("\"amount\":100", audit.NewValue);
        Assert.Contains("\"balanceDue\":200", audit.NewValue);
        Assert.Contains("Settlement", audit.NewValue);
    }

    // ---------- What is refused ----------

    [Fact]
    public async Task Pay_MoreThanTheBalance_IsRefused()
    {
        await OpenShiftAsync();
        var ticket = AddTicket(total: 300, balanceDue: 100);

        var result = await _payments.PayAsync(_cashier.Id, ticket.Id, 100.01m, PaymentMethod.Cash);

        Assert.Equal(PaymentError.AmountExceedsBalance, result.Error);
        Assert.Empty((await ReloadAsync(ticket)).Payments);
    }

    [Fact]
    public async Task Pay_ATicketWithNothingToPay_IsRefused()
    {
        await OpenShiftAsync();
        var ticket = AddTicket(total: 300, balanceDue: 0);

        Assert.Equal(PaymentError.AmountExceedsBalance, (await _payments.PayAsync(_cashier.Id, ticket.Id, 1, PaymentMethod.Cash)).Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(10.005)] // fractions of a cent cannot be paid
    public async Task Pay_AnInvalidAmount_IsRefused(decimal amount)
    {
        await OpenShiftAsync();
        var ticket = AddTicket(total: 300);

        Assert.Equal(PaymentError.InvalidAmount, (await _payments.PayAsync(_cashier.Id, ticket.Id, amount, PaymentMethod.Cash)).Error);
    }

    [Theory]
    [InlineData(PaymentMethod.Wallet)]
    [InlineData(PaymentMethod.PaymentLink)]
    public async Task Pay_WithAMethodNotAcceptedYet_IsRefused(PaymentMethod method)
    {
        await OpenShiftAsync();
        var ticket = AddTicket(total: 300);

        Assert.Equal(PaymentError.MethodNotAccepted, (await _payments.PayAsync(_cashier.Id, ticket.Id, 100, method)).Error);
    }

    [Fact]
    public async Task Pay_WithoutAnOpenShift_IsRefused()
    {
        var ticket = AddTicket(total: 300);
        await OpenShiftAsync(_otherBranchCashier); // another branch's drawer does not count

        Assert.Equal(PaymentError.NoOpenShift, (await _payments.PayAsync(_cashier.Id, ticket.Id, 100, PaymentMethod.Cash)).Error);
    }

    [Fact]
    public async Task Pay_AnotherBranchsTicket_IsRefused()
    {
        await OpenShiftAsync();
        var ticket = AddTicket(total: 300, branch: _otherBranch);

        Assert.Equal(PaymentError.OtherBranch, (await _payments.PayAsync(_cashier.Id, ticket.Id, 100, PaymentMethod.Cash)).Error);
    }

    [Fact]
    public async Task Pay_ACancelledTicket_IsRefused()
    {
        await OpenShiftAsync();
        var ticket = AddTicket(total: 300, status: TicketStatus.Cancelled);

        Assert.Equal(PaymentError.TicketCancelled, (await _payments.PayAsync(_cashier.Id, ticket.Id, 100, PaymentMethod.Cash)).Error);
    }

    [Fact]
    public async Task Pay_AnUnknownTicket_IsRefused()
    {
        await OpenShiftAsync();

        Assert.Equal(PaymentError.TicketNotFound, (await _payments.PayAsync(_cashier.Id, Guid.NewGuid(), 100, PaymentMethod.Cash)).Error);
    }

    [Theory]
    [InlineData("marketing")]
    [InlineData("inactive")]
    public async Task Pay_ByMarketingOrAnInactiveUser_IsRefused(string who)
    {
        await OpenShiftAsync();
        var ticket = AddTicket(total: 300);
        var user = who == "marketing" ? _marketing : _inactive;

        Assert.Equal(PaymentError.NotAllowed, (await _payments.PayAsync(user.Id, ticket.Id, 100, PaymentMethod.Cash)).Error);
    }

    [Fact]
    public async Task Pay_TwoAtTheSameTime_CannotPayMoreThanTheBalance()
    {
        await OpenShiftAsync();
        var ticket = AddTicket(total: 100);

        var results = await Task.WhenAll(
            _payments.PayAsync(_cashier.Id, ticket.Id, 60, PaymentMethod.Cash),
            _payments.PayAsync(_secondCashier.Id, ticket.Id, 60, PaymentMethod.Card));

        Assert.Single(results, r => r.Succeeded);
        Assert.Single(results, r => r.Error == PaymentError.AmountExceedsBalance);
        Assert.Equal(40m, (await ReloadAsync(ticket)).BalanceDue);
    }

    [Fact]
    public async Task Pay_AfterTheShiftWasClosed_IsRefused()
    {
        await OpenShiftAsync();
        var ticket = AddTicket(total: 300);
        await _shifts.CloseAsync(_cashier.Id, 500);

        Assert.Equal(PaymentError.NoOpenShift, (await _payments.PayAsync(_cashier.Id, ticket.Id, 100, PaymentMethod.Cash)).Error);
    }
}
