using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SevenPos.Core.Catalog;
using SevenPos.Core.Guests;
using SevenPos.Core.Organization;
using SevenPos.Core.Tickets;

namespace SevenPos.Core.Tests;

/// <summary>One test per rule in docs/features/ticket-wizard.md.</summary>
public class TicketServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    // 21:00 on Nov 26 in Mexico City, already Nov 27 in UTC: label dates must use the local day.
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 27, 3, 0, 0, TimeSpan.Zero));
    private readonly TicketService _tickets;

    private readonly PriceList _north = new() { Name = "Lista Norte" };
    private readonly Branch _branch, _otherBranch;
    private readonly Category _shirts = new() { Name = "Camisas" };
    private readonly Product _shirt, _jeans, _unpriced, _oldCoat;
    private readonly Modifier _starch, _express, _whiteSole;
    private readonly User _cashier, _otherCashier, _marketing, _formerCashier;
    private readonly Guest _guest, _formerGuest;

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    public TicketServiceTests()
    {
        _tickets = new TicketService(_database, new PriceListService(_database, _clock), _clock);

        _branch = new Branch { Name = "Norte 1", Code = "N1", PriceListId = _north.Id };
        _otherBranch = new Branch { Name = "Norte 2", Code = "N2", PriceListId = _north.Id };
        _starch = new Modifier { Name = "Almidón", ChargeType = ChargeType.Fixed, ChargeValue = 10, SortOrder = 1 };
        _whiteSole = new Modifier { Name = "Suela blanca", ChargeType = ChargeType.Fixed, ChargeValue = 40, SortOrder = 3 };
        _express = new Modifier { Name = "Servicio Express", ChargeType = ChargeType.Percentage, ChargeValue = 50, SortOrder = 100 };
        _shirt = NewProduct("Camisa", 50, _starch, _express);
        _jeans = NewProduct("Jeans", 60, _express);
        _unpriced = NewProduct("Saco", 90);
        _oldCoat = NewProduct("Abrigo viejo", 90);
        _oldCoat.IsActive = false;
        _cashier = NewUser("Carlos", UserRole.Cashier, _branch);
        _otherCashier = NewUser("Olga", UserRole.Cashier, _otherBranch);
        _marketing = NewUser("Mónica", UserRole.Marketing, _branch);
        _formerCashier = NewUser("Fernando", UserRole.Cashier, _branch);
        _formerCashier.IsActive = false;
        _guest = new Guest { Phone = "5512345678", FullName = "Ana López" };
        _formerGuest = new Guest { Phone = "5587654321", FullName = "Baja", IsActive = false };

        using var db = _database.CreateDbContext();
        db.PriceLists.Add(_north);
        db.Branches.AddRange(_branch, _otherBranch);
        db.Categories.Add(_shirts);
        db.Modifiers.AddRange(_starch, _whiteSole, _express);
        db.Products.AddRange(_shirt, _jeans, _unpriced, _oldCoat);
        db.Users.AddRange(_cashier, _otherCashier, _marketing, _formerCashier);
        db.Guests.AddRange(_guest, _formerGuest);
        foreach (var (product, price) in new[] { (_shirt, 55m), (_jeans, 60m), (_oldCoat, 90m) })
        {
            db.PriceListItems.Add(new PriceListItem
            {
                PriceListId = _north.Id, ProductId = product.Id, Price = price,
                EffectiveFromUtc = Now.AddDays(-1), SetByUserId = _marketing.Id,
            });
        }
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    private Product NewProduct(string name, decimal basePrice, params Modifier[] modifiers) => new()
    {
        Name = name, BasePrice = basePrice, CategoryId = _shirts.Id,
        Modifiers = modifiers.Select(m => new ProductModifier { ModifierId = m.Id }).ToList(),
    };

    private static User NewUser(string name, UserRole role, Branch branch) =>
        new() { Name = name, PinHash = "x", Role = role, BranchId = branch.Id };

    private static NewGarment Garment(Product product, params Modifier[] modifiers) =>
        new(product.Id, modifiers.Select(m => m.Id).ToList());

    private Task<TicketCreation> CreateAsync(params NewGarment[] garments) =>
        _tickets.CreateAsync(new NewTicket(_cashier.Id, _guest.Id, garments));

    // ---------- What a ticket stores ----------

    [Fact]
    public async Task Create_UsesTheBranchListPrice_NotTheBasePrice()
    {
        var result = await CreateAsync(Garment(_shirt));

        Assert.True(result.Succeeded);
        var garment = Assert.Single(result.Ticket!.Garments);
        Assert.Equal(55m, garment.ListPrice);
        Assert.Equal(55m, garment.PriceCharged);
        Assert.Equal(_shirt.Id, garment.ProductId);
        Assert.Equal(GarmentStatus.Dirty, garment.Status);
    }

    [Fact]
    public async Task Create_StoresTheTicketHeader()
    {
        var ticket = (await CreateAsync(Garment(_shirt))).Ticket!;

        await using var db = _database.CreateDbContext();
        var saved = await db.Tickets.SingleAsync();
        Assert.Equal(ticket.Id, saved.Id);
        Assert.Equal(_branch.Id, saved.BranchId);
        Assert.Equal(_cashier.Id, saved.CashierId);
        Assert.Equal(_guest.Id, saved.GuestId);
        Assert.Equal(Now, saved.CreatedAtUtc);
        Assert.Equal(TicketStatus.Received, saved.Status);
        Assert.Equal(TicketOrigin.Counter, saved.Origin);
        Assert.Null(saved.ShiftId);
    }

    [Fact]
    public async Task Create_FreezesEachModifierCharge_InSortOrder()
    {
        // $55 + Almidón $10 + Express 50% of the $55 list price = $27.50 → $92.50 (ADR 0012).
        // The request lists Express first on purpose: charges are stored in SortOrder.
        var garment = (await CreateAsync(Garment(_shirt, _express, _starch))).Ticket!.Garments.Single();

        Assert.Equal(92.50m, garment.PriceCharged);
        Assert.Equal(
            [(_starch.Id, 10m), (_express.Id, 27.50m)],
            garment.Modifiers.Select(m => (m.ModifierId, m.ChargeApplied)));
    }

    [Fact]
    public async Task Create_SameModifierTwice_IsChargedOnce()
    {
        var garment = (await CreateAsync(Garment(_shirt, _starch, _starch))).Ticket!.Garments.Single();

        Assert.Equal(65m, garment.PriceCharged);
        Assert.Single(garment.Modifiers);
    }

    [Fact]
    public async Task Create_OneRowPerPhysicalGarment_TotalIsTheirSum()
    {
        var ticket = (await CreateAsync(Garment(_shirt), Garment(_shirt), Garment(_jeans, _express))).Ticket!;

        Assert.Equal(3, ticket.Garments.Count);
        Assert.Equal(55m + 55m + 90m, ticket.Subtotal);
        Assert.Equal(0m, ticket.Discounts);
        Assert.Equal(200m, ticket.Total);
        Assert.Equal(200m, ticket.BalanceDue);
    }

    [Fact]
    public async Task Create_PricesStayFrozen_WhenTheListChangesLater()
    {
        var ticket = (await CreateAsync(Garment(_shirt))).Ticket!;
        await new PriceListService(_database, _clock).SetPriceAsync(_marketing.Id, _north.Id, _shirt.Id, 70m);

        var saved = await _tickets.GetAsync(ticket.Id);

        Assert.Equal(55m, saved!.Garments.Single().PriceCharged);
        Assert.Equal(55m, saved.Total);
    }

    [Fact]
    public async Task Create_RecordsTheGuestVisit()
    {
        await CreateAsync(Garment(_shirt));

        await using var db = _database.CreateDbContext();
        Assert.Equal(Now, (await db.Guests.SingleAsync(g => g.Id == _guest.Id)).LastVisitUtc);
    }

    [Fact]
    public async Task Create_IsAudited()
    {
        var ticket = (await CreateAsync(Garment(_shirt), Garment(_jeans))).Ticket!;

        await using var db = _database.CreateDbContext();
        var log = await db.AuditLogs.SingleAsync(a => a.Action == "TICKET_CREATED");
        Assert.Equal(_cashier.Id, log.UserId);
        Assert.Equal(_branch.Id, log.BranchId);
        Assert.Equal(Now, log.CreatedAtUtc);
        var details = JsonDocument.Parse(log.NewValue!).RootElement;
        Assert.Equal(ticket.Id, details.GetProperty("ticketId").GetGuid());
        Assert.Equal(1, details.GetProperty("folio").GetInt32());
        Assert.Equal(115m, details.GetProperty("total").GetDecimal());
        Assert.Equal(2, details.GetProperty("garments").GetInt32());
    }

    // ---------- Numbers: folio, public order ID, garment labels ----------

    [Fact]
    public async Task Folio_StartsAtOne_AndCountsPerBranch()
    {
        var first = (await CreateAsync(Garment(_shirt))).Ticket!;
        var second = (await CreateAsync(Garment(_shirt))).Ticket!;
        var otherBranch = (await _tickets.CreateAsync(new NewTicket(_otherCashier.Id, _guest.Id, [Garment(_shirt)]))).Ticket!;

        Assert.Equal(1, first.Folio);
        Assert.Equal(2, second.Folio);
        Assert.Equal(1, otherBranch.Folio);
    }

    [Fact]
    public async Task Folio_TicketsCreatedAtTheSameTime_GetDifferentFolios()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => CreateAsync(Garment(_shirt))));

        Assert.All(results, r => Assert.True(r.Succeeded));
        Assert.Equal([1, 2, 3, 4, 5], results.Select(r => r.Ticket!.Folio).Order());
    }

    [Fact]
    public async Task GarmentCodes_AreBranchLocalDateFolioAndSequence()
    {
        await CreateAsync(Garment(_jeans));
        var ticket = (await CreateAsync(Garment(_shirt), Garment(_shirt, _starch))).Ticket!;

        Assert.Equal(
            ["N1-20261126-000002-01", "N1-20261126-000002-02"],
            ticket.Garments.Select(g => g.Code));
    }

    [Fact]
    public async Task PublicOrderId_IsEightUnambiguousCharacters_AndUniquePerTicket()
    {
        var ids = new List<string>();
        for (var i = 0; i < 5; i++)
            ids.Add((await CreateAsync(Garment(_shirt))).Ticket!.PublicOrderId);

        Assert.All(ids, id => Assert.Matches("^[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{8}$", id));
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    // ---------- What is refused ----------

    [Fact]
    public async Task Create_WithoutGarments_IsRefused()
    {
        Assert.Equal(TicketError.NoGarments, (await CreateAsync()).Error);
    }

    [Theory]
    [InlineData("marketing")]
    [InlineData("inactive")]
    [InlineData("unknown")]
    public async Task Create_ByMarketing_InactiveOrUnknownUser_IsRefused(string who)
    {
        var userId = who switch
        {
            "marketing" => _marketing.Id,
            "inactive" => _formerCashier.Id,
            _ => Guid.NewGuid(),
        };

        var result = await _tickets.CreateAsync(new NewTicket(userId, _guest.Id, [Garment(_shirt)]));

        Assert.Equal(TicketError.NotAllowed, result.Error);
    }

    [Fact]
    public async Task Create_ForAnInactiveOrUnknownGuest_IsRefused()
    {
        Assert.Equal(TicketError.GuestNotFound,
            (await _tickets.CreateAsync(new NewTicket(_cashier.Id, _formerGuest.Id, [Garment(_shirt)]))).Error);
        Assert.Equal(TicketError.GuestNotFound,
            (await _tickets.CreateAsync(new NewTicket(_cashier.Id, Guid.NewGuid(), [Garment(_shirt)]))).Error);
    }

    [Fact]
    public async Task Create_ProductWithoutABranchPrice_IsRefused()
    {
        Assert.Equal(TicketError.ProductNotAvailable, (await CreateAsync(Garment(_unpriced))).Error);
    }

    [Fact]
    public async Task Create_InactiveProduct_IsRefused_EvenWithAPrice()
    {
        Assert.Equal(TicketError.ProductNotAvailable, (await CreateAsync(Garment(_oldCoat))).Error);
    }

    [Fact]
    public async Task Create_ModifierNotOfferedForThatProduct_IsRefused()
    {
        // Suela blanca is for sneakers, not shirts.
        Assert.Equal(TicketError.ModifierNotAllowed, (await CreateAsync(Garment(_shirt, _whiteSole))).Error);
    }

    [Fact]
    public async Task Create_Refused_SavesNothing()
    {
        await CreateAsync(Garment(_shirt), Garment(_unpriced));

        await using var db = _database.CreateDbContext();
        Assert.False(await db.Tickets.AnyAsync());
        Assert.False(await db.Garments.AnyAsync());
        Assert.False(await db.AuditLogs.AnyAsync());
    }

    // ---------- Reading a ticket ----------

    [Fact]
    public async Task Get_ReturnsGuestGarmentsProductsAndExtras_InLabelOrder()
    {
        var created = (await CreateAsync(Garment(_jeans), Garment(_shirt, _starch))).Ticket!;

        var ticket = await _tickets.GetAsync(created.Id);

        Assert.Equal("Ana López", ticket!.Guest!.FullName);
        Assert.Equal(["Jeans", "Camisa"], ticket.Garments.Select(g => g.Product!.Name));
        Assert.Equal(["Almidón"], ticket.Garments[1].Modifiers.Select(m => m.Modifier!.Name));
    }

    [Fact]
    public async Task Get_UnknownTicket_ReturnsNull()
    {
        Assert.Null(await _tickets.GetAsync(Guid.NewGuid()));
    }
}
