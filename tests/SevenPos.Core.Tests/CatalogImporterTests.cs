using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SevenPos.Core.Catalog;
using SevenPos.Core.Organization;

namespace SevenPos.Core.Tests;

/// <summary>Rules in docs/features/real-catalog.md: loading Marketing's price lists.</summary>
public class CatalogImporterTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 26, 15, 0, 0, TimeSpan.Zero));
    private readonly CatalogImporter _importer;
    private readonly Branch _branch = new() { Name = "Norte", Code = "N1" };
    private readonly User _marketing, _cashier;

    private const string Products = """
        service,group,product,list_a,list_aa,list_b,list_c,notes
        Tintorería,Ropa de vestir / Casual,CAMISA,89,99,79,69,
        Tintorería,Ropa de cama y blancos,EDREDON,235,245,225,215,
        Tintorería,Política de calidad,REPETICION DE LIMPIEZA PRENDA,0,0,0,0,
        Planchado,Ropa de vestir / Casual,CAMISA,49,,,,
        Planchado,Ropa de vestir / Casual,SUDADERA,49,,,,listed twice
        Planchado,Ropa de vestir / Casual,SUDADERA,79,,,,listed twice
        Sastrería,Sastrería,DOBLADILLO,159,,,,
        """;

    private const string Modifiers = """
        service,step,group,option,charge_type,value,notes
        Tintorería,1,Tamaño,Mediano,percent,15,
        Tintorería,1,Tamaño,Matrimonial,percent,15,
        Tintorería,1,Tamaño,Queen / King,percent,20,
        Tintorería,8,Urgente,Urgente,percent,50,
        Tintorería,6,Almidón,Ligero,percent,5,
        Planchado,1,Tamaño,Mediano,percent,15,
        Planchado,4,Urgente,Urgente,percent,50,
        Sastrería,1,Tamaño,Mediano,percent,15,
        """;

    public CatalogImporterTests()
    {
        _importer = new CatalogImporter(_database, _clock);
        _marketing = new User { Name = "Mónica", PinHash = "x", Role = UserRole.Marketing, BranchId = _branch.Id };
        _cashier = new User { Name = "Carlos", PinHash = "x", Role = UserRole.Cashier, BranchId = _branch.Id };
        using var db = _database.CreateDbContext();
        db.Branches.Add(_branch);
        db.Users.AddRange(_marketing, _cashier);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    private Task<CatalogImportResult> ImportAsync(Guid? actor = null, params string[] lines) =>
        _importer.ImportAsync(actor ?? _marketing.Id, new StringReader(Products), new StringReader(Modifiers),
            lines.Length > 0 ? lines : ["Tintorería", "Planchado"]);

    [Fact]
    public async Task Import_CreatesOnlyTheRequestedServiceLines_InThatOrder()
    {
        await ImportAsync();

        await using var db = _database.CreateDbContext();
        Assert.Equal(["Tintorería", "Planchado"], await db.ServiceLines.OrderBy(l => l.SortOrder).Select(l => l.Name).ToListAsync());
        Assert.False(await db.Products.AnyAsync(p => p.Name == "DOBLADILLO"));
    }

    [Fact]
    public async Task Import_CreatesGroupsWithTheirStepNumber_AndTheirOptions()
    {
        await ImportAsync();

        await using var db = _database.CreateDbContext();
        var groups = await db.ModifierGroups
            .Where(g => db.ServiceLines.Any(l => l.Id == g.ServiceLineId && l.Name == "Tintorería"))
            .Include(g => g.Modifiers)
            .OrderBy(g => g.SortOrder)
            .ToListAsync();
        Assert.Equal([("Tamaño", 1), ("Almidón", 6), ("Urgente", 8)], groups.Select(g => (g.Name, g.SortOrder)));
        Assert.Equal(["Mediano", "Matrimonial", "Queen / King"], groups[0].Modifiers.OrderBy(m => m.SortOrder).Select(m => m.Name));
        var queen = groups[0].Modifiers.Single(m => m.Name == "Queen / King");
        Assert.Equal((ChargeType.Percentage, 20m), (queen.ChargeType, queen.ChargeValue));
    }

    [Fact]
    public async Task Import_SameGarmentInTwoLines_IsTwoProducts_WithTheirOwnPrices()
    {
        await ImportAsync();

        await using var db = _database.CreateDbContext();
        var shirts = await db.Products.Include(p => p.ServiceLine).Where(p => p.Name == "CAMISA").ToListAsync();
        Assert.Equal(["Planchado", "Tintorería"], shirts.Select(p => p.ServiceLine!.Name).Order());
        Assert.Equal(49m, shirts.Single(p => p.ServiceLine!.Name == "Planchado").BasePrice);
    }

    [Fact]
    public async Task Import_PricesGoToEachListThatHasOne()
    {
        await ImportAsync();

        await using var db = _database.CreateDbContext();
        var prices = await db.PriceListItems
            .Where(i => i.Product!.Name == "CAMISA")
            .Select(i => new { List = db.PriceLists.Single(l => l.Id == i.PriceListId).Name, Line = i.Product!.ServiceLine!.Name, i.Price, i.EffectiveFromUtc, i.SetByUserId })
            .ToListAsync();
        Assert.Equal(
            ["Planchado/Lista A/49.00", "Tintorería/Lista A/89.00", "Tintorería/Lista AA/99.00", "Tintorería/Lista B/79.00", "Tintorería/Lista C/69.00"],
            prices.Select(p => $"{p.Line}/{p.List}/{p.Price:0.00}").Order());
        Assert.All(prices, p => Assert.Equal(_clock.GetUtcNow().UtcDateTime, p.EffectiveFromUtc));
        Assert.All(prices, p => Assert.Equal(_marketing.Id, p.SetByUserId));
    }

    [Fact]
    public async Task Import_BedSizes_AreOfferedOnlyToBedItems()
    {
        // Client: only bed items get Matrimonial and Queen / King.
        await ImportAsync();

        Assert.Equal(["Mediano", "Ligero", "Urgente"], await OptionsOfAsync("Tintorería", "CAMISA"));
        Assert.Equal(["Mediano", "Matrimonial", "Queen / King", "Ligero", "Urgente"], await OptionsOfAsync("Tintorería", "EDREDON"));
    }

    [Fact]
    public async Task Import_SkipsZeroPricesAndDuplicates_AndReportsThem()
    {
        var result = await ImportAsync();

        Assert.Equal(4, result.Products);
        Assert.Equal(
            ["Planchado · SUDADERA ($79): repeated product", "Tintorería · REPETICION DE LIMPIEZA PRENDA: price $0"],
            result.Skipped.Order());
        await using var db = _database.CreateDbContext();
        Assert.Equal(49m, (await db.Products.SingleAsync(p => p.Name == "SUDADERA")).BasePrice);
    }

    [Fact]
    public async Task Import_Twice_AddsNothingTheSecondTime()
    {
        await ImportAsync();
        var second = await ImportAsync();

        Assert.Equal((0, 0, 0, 0), (second.Products, second.Groups, second.Options, second.Prices));
        await using var db = _database.CreateDbContext();
        Assert.Equal(10, await db.PriceListItems.CountAsync());
    }

    [Fact]
    public async Task Import_ReusesAnExistingListWithTheSameName()
    {
        await using (var db = _database.CreateDbContext())
        {
            db.PriceLists.Add(new PriceList { Name = "Lista AA" });
            await db.SaveChangesAsync();
        }

        await ImportAsync();

        await using var check = _database.CreateDbContext();
        Assert.Equal(1, await check.PriceLists.CountAsync(l => l.Name == "Lista AA"));
    }

    [Fact]
    public async Task Import_ByAnyoneButMarketing_IsRefused()
    {
        var result = await ImportAsync(_cashier.Id);

        Assert.Equal(CatalogImportError.NotAllowed, result.Error);
        await using var db = _database.CreateDbContext();
        Assert.False(await db.Products.AnyAsync());
    }

    [Fact]
    public async Task Import_IsAudited()
    {
        await ImportAsync();

        await using var db = _database.CreateDbContext();
        var log = await db.AuditLogs.SingleAsync(a => a.Action == "CATALOG_IMPORTED");
        Assert.Equal(_marketing.Id, log.UserId);
        Assert.Equal(4, JsonDocument.Parse(log.NewValue!).RootElement.GetProperty("products").GetInt32());
    }

    [Fact]
    public async Task Import2026_LoadsTheReadyServiceLines()
    {
        var result = await _importer.Import2026Async(_marketing.Id);

        Assert.True(result.Succeeded);
        Assert.Equal((136, 16, 41, 307), (result.Products, result.Groups, result.Options, result.Prices));
        Assert.Equal(3, result.Skipped.Count);
    }

    private async Task<List<string>> OptionsOfAsync(string line, string product)
    {
        await using var db = _database.CreateDbContext();
        return await db.ProductModifiers
            .Where(pm => pm.Product!.Name == product && pm.Product.ServiceLine!.Name == line)
            .OrderBy(pm => pm.Modifier!.Group!.SortOrder).ThenBy(pm => pm.Modifier!.SortOrder)
            .Select(pm => pm.Modifier!.Name)
            .ToListAsync();
    }
}
