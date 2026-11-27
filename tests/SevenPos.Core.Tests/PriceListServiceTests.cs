using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SevenPos.Core.Catalog;
using SevenPos.Core.Organization;

namespace SevenPos.Core.Tests;

/// <summary>One test per rule in docs/features/price-lists.md.</summary>
public class PriceListServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 26, 15, 0, 0, TimeSpan.Zero));
    private readonly PriceListService _prices;

    private readonly PriceList _north = new() { Name = "Lista Norte" };
    private readonly PriceList _south = new() { Name = "Lista Sur" };
    private readonly Branch _northBranch, _southBranch, _branchWithoutList;
    private readonly ServiceLine _line = new() { Name = "Tintorería" };
    private readonly ModifierGroup _extras;
    private readonly Category _shirts = new() { Name = "Camisas" };
    private readonly Product _shirt, _jeans, _oldCoat;
    private readonly User _marketing, _cashier;

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    public PriceListServiceTests()
    {
        _prices = new PriceListService(_database, _clock);
        _northBranch = new Branch { Name = "Norte 1", Code = "N1", PriceListId = _north.Id };
        _southBranch = new Branch { Name = "Sur 1", Code = "S1", PriceListId = _south.Id };
        _branchWithoutList = new Branch { Name = "Nueva", Code = "NVA" };
        _extras = new ModifierGroup { ServiceLineId = _line.Id, Name = "Extras", SortOrder = 1 };
        _shirt = new Product { ServiceLineId = _line.Id, Name = "Camisa", BasePrice = 50, CategoryId = _shirts.Id };
        _jeans = new Product { ServiceLineId = _line.Id, Name = "Jeans", BasePrice = 60, CategoryId = _shirts.Id };
        _oldCoat = new Product { ServiceLineId = _line.Id, Name = "Abrigo viejo", BasePrice = 90, CategoryId = _shirts.Id, IsActive = false };
        _marketing = new User { Name = "Mónica", PinHash = "x", Role = UserRole.Marketing, BranchId = _northBranch.Id };
        _cashier = new User { Name = "Carlos", PinHash = "x", Role = UserRole.Cashier, BranchId = _northBranch.Id };

        using var db = _database.CreateDbContext();
        db.PriceLists.AddRange(_north, _south);
        db.Branches.AddRange(_northBranch, _southBranch, _branchWithoutList);
        db.ServiceLines.Add(_line);
        db.ModifierGroups.Add(_extras);
        db.Categories.Add(_shirts);
        db.Products.AddRange(_shirt, _jeans, _oldCoat);
        db.Users.AddRange(_marketing, _cashier);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    /// <summary>Inserts a price row directly, bypassing the rules, to set up a scenario.</summary>
    private void AddPrice(PriceList list, Product product, decimal price, DateTime effectiveFromUtc)
    {
        using var db = _database.CreateDbContext();
        db.PriceListItems.Add(new PriceListItem
        {
            PriceListId = list.Id, ProductId = product.Id, Price = price,
            EffectiveFromUtc = effectiveFromUtc, SetByUserId = _marketing.Id,
        });
        db.SaveChanges();
    }

    // ---------- What a branch sells ----------

    [Fact]
    public async Task BranchCatalog_OnlyHasProductsWithAPriceInTheBranchList()
    {
        AddPrice(_north, _shirt, 55, Now.AddDays(-1));

        var catalog = await _prices.GetBranchCatalogAsync(_northBranch.Id);

        Assert.Equal(["Camisa"], catalog.Select(e => e.Product.Name));
        Assert.Equal(55m, catalog[0].Price);
    }

    [Fact]
    public async Task BranchCatalog_UsesTheLatestPriceAlreadyInEffect_NotAFutureOne()
    {
        AddPrice(_north, _shirt, 50, Now.AddDays(-30));
        AddPrice(_north, _shirt, 55, Now.AddDays(-1));
        AddPrice(_north, _shirt, 60, Now.AddDays(7));

        var catalog = await _prices.GetBranchCatalogAsync(_northBranch.Id);

        Assert.Equal(55m, catalog.Single().Price);
    }

    [Fact]
    public async Task BranchCatalog_ScheduledPrice_AppliesWhenItsTimeComes()
    {
        AddPrice(_north, _shirt, 55, Now.AddDays(-1));
        AddPrice(_north, _shirt, 60, Now.AddDays(7));

        _clock.Advance(TimeSpan.FromDays(7));

        Assert.Equal(60m, (await _prices.GetBranchCatalogAsync(_northBranch.Id)).Single().Price);
    }

    [Fact]
    public async Task BranchCatalog_EachBranchSeesItsOwnListPrices()
    {
        AddPrice(_north, _shirt, 55, Now.AddDays(-1));
        AddPrice(_south, _shirt, 48, Now.AddDays(-1));

        Assert.Equal(55m, (await _prices.GetBranchCatalogAsync(_northBranch.Id)).Single().Price);
        Assert.Equal(48m, (await _prices.GetBranchCatalogAsync(_southBranch.Id)).Single().Price);
    }

    [Fact]
    public async Task BranchCatalog_BranchWithoutAList_SellsNothing()
    {
        AddPrice(_north, _shirt, 55, Now.AddDays(-1));

        Assert.Empty(await _prices.GetBranchCatalogAsync(_branchWithoutList.Id));
    }

    [Fact]
    public async Task BranchCatalog_HidesInactiveProducts_EvenWithAPrice()
    {
        AddPrice(_north, _oldCoat, 90, Now.AddDays(-1));

        Assert.Empty(await _prices.GetBranchCatalogAsync(_northBranch.Id));
    }

    [Fact]
    public async Task BranchCatalog_HidesInactiveModifiers()
    {
        await using (var db = _database.CreateDbContext())
        {
            var starch = new Modifier { ModifierGroupId = _extras.Id, Name = "Almidón", ChargeType = ChargeType.Fixed, ChargeValue = 10, SortOrder = 1 };
            var oldExtra = new Modifier { ModifierGroupId = _extras.Id, Name = "Perfume", ChargeType = ChargeType.Fixed, ChargeValue = 5, SortOrder = 2, IsActive = false };
            var removedLink = new Modifier { ModifierGroupId = _extras.Id, Name = "Doblado", ChargeType = ChargeType.Fixed, ChargeValue = 8, SortOrder = 3 };
            db.Modifiers.AddRange(starch, oldExtra, removedLink);
            db.ProductModifiers.AddRange(
                new ProductModifier { ProductId = _shirt.Id, ModifierId = starch.Id },
                new ProductModifier { ProductId = _shirt.Id, ModifierId = oldExtra.Id },
                new ProductModifier { ProductId = _shirt.Id, ModifierId = removedLink.Id, IsActive = false });
            await db.SaveChangesAsync();
        }
        AddPrice(_north, _shirt, 55, Now.AddDays(-1));

        var shirt = (await _prices.GetBranchCatalogAsync(_northBranch.Id)).Single().Product;

        Assert.Equal(["Almidón"], shirt.Modifiers.Select(pm => pm.Modifier!.Name));
    }

    [Fact]
    public async Task BranchCatalog_IncludesCategoryAndModifiers_SortedByCategoryThenName()
    {
        await using (var db = _database.CreateDbContext())
        {
            var pants = new Category { Name = "Pantalones" };
            var starch = new Modifier { ModifierGroupId = _extras.Id, Name = "Almidón", ChargeType = ChargeType.Fixed, ChargeValue = 10, SortOrder = 1 };
            db.Categories.Add(pants);
            db.Modifiers.Add(starch);
            db.ProductModifiers.Add(new ProductModifier { ProductId = _shirt.Id, ModifierId = starch.Id });
            (await db.Products.SingleAsync(p => p.Id == _jeans.Id)).CategoryId = pants.Id;
            await db.SaveChangesAsync();
        }
        AddPrice(_north, _jeans, 60, Now.AddDays(-1));
        AddPrice(_north, _shirt, 55, Now.AddDays(-1));

        var catalog = await _prices.GetBranchCatalogAsync(_northBranch.Id);

        Assert.Equal(["Camisas/Camisa", "Pantalones/Jeans"], catalog.Select(e => $"{e.Product.Category!.Name}/{e.Product.Name}"));
        Assert.Equal("Almidón", catalog[0].Product.Modifiers.Single().Modifier!.Name);
        Assert.Equal("Extras", catalog[0].Product.Modifiers.Single().Modifier!.Group!.Name);
        Assert.Equal("Tintorería", catalog[0].Product.ServiceLine!.Name);
    }

    // ---------- What Marketing manages ----------

    [Fact]
    public async Task ListPrices_ShowsEveryActiveProduct_WithCurrentAndNextPrice()
    {
        AddPrice(_north, _shirt, 55, Now.AddDays(-1));
        AddPrice(_north, _shirt, 60, Now.AddDays(7));

        var rows = await _prices.GetListPricesAsync(_north.Id);

        Assert.Equal(["Camisa", "Jeans"], rows.Select(r => r.Product.Name));
        Assert.Equal((55m, 60m, Now.AddDays(7)), (rows[0].CurrentPrice, rows[0].NextPrice, rows[0].NextEffectiveFromUtc));
        Assert.Equal(((decimal?)null, (decimal?)null), (rows[1].CurrentPrice, rows[1].NextPrice));
    }

    [Fact]
    public async Task SetPrice_ByMarketing_AddsARow_AndKeepsTheHistory()
    {
        AddPrice(_north, _shirt, 50, Now.AddDays(-30));

        var result = await _prices.SetPriceAsync(_marketing.Id, _north.Id, _shirt.Id, 55);

        Assert.True(result.Succeeded);
        Assert.Equal(55m, (await _prices.GetBranchCatalogAsync(_northBranch.Id)).Single().Price);
        var history = await _prices.GetHistoryAsync(_north.Id, _shirt.Id);
        Assert.Equal([55m, 50m], history.Select(h => h.Price));
        Assert.Equal("Mónica", history[0].SetBy);
    }

    [Fact]
    public async Task SetPrice_CanBeScheduledForTheFuture()
    {
        AddPrice(_north, _shirt, 50, Now.AddDays(-30));

        var result = await _prices.SetPriceAsync(_marketing.Id, _north.Id, _shirt.Id, 60, Now.AddDays(7));

        Assert.True(result.Succeeded);
        Assert.Equal(50m, (await _prices.GetBranchCatalogAsync(_northBranch.Id)).Single().Price);
    }

    [Theory]
    [InlineData(UserRole.Cashier)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Admin)]
    public async Task SetPrice_ByAnyRoleButMarketing_IsNotAllowed_AndSavesNothing(UserRole role)
    {
        await using (var db = _database.CreateDbContext())
        {
            (await db.Users.SingleAsync(u => u.Id == _cashier.Id)).Role = role;
            await db.SaveChangesAsync();
        }

        var result = await _prices.SetPriceAsync(_cashier.Id, _north.Id, _shirt.Id, 1);

        Assert.Equal(PriceChangeError.NotAllowed, result.Error);
        await using var check = _database.CreateDbContext();
        Assert.Equal(0, await check.PriceListItems.CountAsync());
    }

    [Fact]
    public async Task SetPrice_ByInactiveMarketingUser_IsNotAllowed()
    {
        await using (var db = _database.CreateDbContext())
        {
            (await db.Users.SingleAsync(u => u.Id == _marketing.Id)).IsActive = false;
            await db.SaveChangesAsync();
        }

        var result = await _prices.SetPriceAsync(_marketing.Id, _north.Id, _shirt.Id, 55);

        Assert.Equal(PriceChangeError.NotAllowed, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10.005)]
    public async Task SetPrice_ZeroNegativeOrFractionOfACent_IsInvalid(decimal price)
    {
        var result = await _prices.SetPriceAsync(_marketing.Id, _north.Id, _shirt.Id, price);

        Assert.Equal(PriceChangeError.InvalidPrice, result.Error);
    }

    [Fact]
    public async Task SetPrice_InThePast_IsRejected()
    {
        var result = await _prices.SetPriceAsync(_marketing.Id, _north.Id, _shirt.Id, 55, Now.AddMinutes(-1));

        Assert.Equal(PriceChangeError.EffectiveDateInPast, result.Error);
    }

    [Fact]
    public async Task SetPrice_UnknownListOrProduct_IsRejected()
    {
        Assert.Equal(PriceChangeError.PriceListNotFound,
            (await _prices.SetPriceAsync(_marketing.Id, Guid.NewGuid(), _shirt.Id, 55)).Error);
        Assert.Equal(PriceChangeError.ProductNotFound,
            (await _prices.SetPriceAsync(_marketing.Id, _north.Id, Guid.NewGuid(), 55)).Error);
    }

    [Fact]
    public async Task SetPrice_IsAudited_WithOldAndNewPrice()
    {
        AddPrice(_north, _shirt, 50, Now.AddDays(-30));

        await _prices.SetPriceAsync(_marketing.Id, _north.Id, _shirt.Id, 55);

        await using var db = _database.CreateDbContext();
        var log = await db.AuditLogs.SingleAsync();
        Assert.Equal("PRICE_CHANGED", log.Action);
        Assert.Equal(_marketing.Id, log.UserId);
        Assert.Contains("50", log.OldValue);
        Assert.Contains("55", log.NewValue);
        Assert.Contains("Lista Norte", log.NewValue);
        Assert.Contains("Camisa", log.NewValue);
    }

    [Fact]
    public async Task SetPrice_FirstPriceOfAProduct_HasNoOldValueInTheAudit()
    {
        await _prices.SetPriceAsync(_marketing.Id, _north.Id, _shirt.Id, 55);

        await using var db = _database.CreateDbContext();
        Assert.Null((await db.AuditLogs.SingleAsync()).OldValue);
    }

    [Fact]
    public async Task PriceLists_AreTheActiveOnes_SortedByName()
    {
        await using (var db = _database.CreateDbContext())
        {
            db.PriceLists.Add(new PriceList { Name = "Lista Cerrada", IsActive = false });
            await db.SaveChangesAsync();
        }

        var lists = await _prices.GetPriceListsAsync();

        Assert.Equal(["Lista Norte", "Lista Sur"], lists.Select(l => l.Name));
    }

    // ---------- Which list (area) each branch sells from ----------

    [Fact]
    public async Task Branches_ListsActiveBranches_WithTheirList()
    {
        var branches = await _prices.GetBranchesAsync();

        Assert.Equal(["Norte 1/Lista Norte", "Nueva/", "Sur 1/Lista Sur"],
            branches.Select(b => $"{b.Name}/{b.PriceList?.Name}"));
    }

    [Fact]
    public async Task AssignList_ByMarketing_ChangesWhatTheBranchSells()
    {
        AddPrice(_north, _shirt, 55, Now.AddDays(-1));
        AddPrice(_south, _shirt, 48, Now.AddDays(-1));

        var result = await _prices.AssignBranchListAsync(_marketing.Id, _northBranch.Id, _south.Id);

        Assert.Null(result);
        Assert.Equal(48m, (await _prices.GetBranchCatalogAsync(_northBranch.Id)).Single().Price);
    }

    [Fact]
    public async Task AssignList_ByAnyoneButMarketing_IsRefused()
    {
        Assert.Equal(BranchListError.NotAllowed, await _prices.AssignBranchListAsync(_cashier.Id, _northBranch.Id, _south.Id));

        await using var db = _database.CreateDbContext();
        Assert.Equal(_north.Id, (await db.Branches.SingleAsync(b => b.Id == _northBranch.Id)).PriceListId);
    }

    [Fact]
    public async Task AssignList_UnknownBranchOrInactiveList_IsRefused()
    {
        var closed = new PriceList { Name = "Lista vieja", IsActive = false };
        await using (var db = _database.CreateDbContext())
        {
            db.PriceLists.Add(closed);
            await db.SaveChangesAsync();
        }

        Assert.Equal(BranchListError.BranchNotFound, await _prices.AssignBranchListAsync(_marketing.Id, Guid.NewGuid(), _south.Id));
        Assert.Equal(BranchListError.PriceListNotFound, await _prices.AssignBranchListAsync(_marketing.Id, _northBranch.Id, closed.Id));
    }

    [Fact]
    public async Task AssignList_IsAudited_WithTheOldAndNewList()
    {
        await _prices.AssignBranchListAsync(_marketing.Id, _branchWithoutList.Id, _north.Id);
        await _prices.AssignBranchListAsync(_marketing.Id, _branchWithoutList.Id, _south.Id);

        await using var db = _database.CreateDbContext();
        var changes = await db.AuditLogs.Where(a => a.Action == "BRANCH_PRICE_LIST_CHANGED").ToListAsync();
        Assert.Equal(2, changes.Count);
        var last = changes.Single(a => a.OldValue is not null);
        Assert.Equal(_marketing.Id, last.UserId);
        Assert.Equal(_branchWithoutList.Id, last.BranchId);
        Assert.Contains("Lista Norte", last.OldValue);
        Assert.Contains("Lista Sur", last.NewValue);
    }
}
