using Microsoft.EntityFrameworkCore;

namespace SevenPos.Core.Data;

/// <summary>
/// Sample data for development only (never loaded in production, see Program.cs).
/// Each step checks for its own data, so a database created by an older version of
/// the app is completed instead of skipped.
/// </summary>
public static class SeedData
{
    /// <summary>The line the first sample catalog lived in (and where the migration put older catalogs).</summary>
    private const string OldSampleLine = "General";

    /// <summary>Sample PINs: Admin 1234, Mercadotecnia 5678. Catalog: Marketing's 2026 lists; every branch sells from Lista A.</summary>
    public static async Task LoadAsync(IDbContextFactory<PosDbContext> dbFactory, TimeProvider clock)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await LoadBranchAndAdminAsync(db);
        await EnsureMarketingUserAsync(db);
        await EnsureCatalog2026Async(db, new CatalogImporter(dbFactory, clock));
    }

    private static async Task LoadBranchAndAdminAsync(PosDbContext db)
    {
        if (await db.Branches.AnyAsync()) return;

        var branch = new Branch { Name = "Sucursal Demo", Code = "DEMO" };
        db.Branches.Add(branch);
        db.Users.Add(new User
        {
            Name = "Admin", Role = UserRole.Admin,
            PinHash = PinHasher.Hash("1234"), BranchId = branch.Id,
        });
        await db.SaveChangesAsync();
    }

    private static async Task EnsureMarketingUserAsync(PosDbContext db)
    {
        if (await db.Users.AnyAsync(u => u.Role == UserRole.Marketing)) return;
        var branch = await db.Branches.OrderBy(b => b.Name).FirstAsync();
        db.Users.Add(new User
        {
            Name = "Mercadotecnia", Role = UserRole.Marketing,
            PinHash = PinHasher.Hash("5678"), BranchId = branch.Id,
        });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Loads Marketing's 2026 catalog once. A database from an older version has the first
    /// sample catalog (Camisa de vestir, Almidón…) in the "General" line: it is deactivated,
    /// never deleted, and every branch moves to Lista A.
    /// </summary>
    private static async Task EnsureCatalog2026Async(PosDbContext db, CatalogImporter importer)
    {
        if (await db.ServiceLines.AnyAsync(l => l.Name == "Tintorería")) return;

        var oldLine = await db.ServiceLines.SingleOrDefaultAsync(l => l.Name == OldSampleLine);
        if (oldLine is not null)
        {
            oldLine.IsActive = false;
            await db.Products.Where(p => p.ServiceLineId == oldLine.Id).ForEachAsync(p => p.IsActive = false);
            await db.ModifierGroups.Where(g => g.ServiceLineId == oldLine.Id).Include(g => g.Modifiers)
                .ForEachAsync(g => { g.IsActive = false; g.Modifiers.ForEach(m => m.IsActive = false); });
            await db.PriceLists.Where(l => l.Name == "Lista General").ForEachAsync(l => l.IsActive = false);
            await db.SaveChangesAsync();
        }

        var marketing = await db.Users.FirstAsync(u => u.Role == UserRole.Marketing);
        await importer.Import2026Async(marketing.Id);

        var listA = await db.PriceLists.SingleAsync(l => l.Name == "Lista A");
        await db.Branches.ForEachAsync(b => b.PriceListId = listA.Id);
        await db.SaveChangesAsync();
    }
}
