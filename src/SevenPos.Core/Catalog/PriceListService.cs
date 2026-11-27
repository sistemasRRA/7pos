using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SevenPos.Core.Data;

namespace SevenPos.Core.Catalog;

/// <summary>A product a branch can sell, with its current list price (IVA included).</summary>
public record CatalogEntry(Product Product, decimal Price);

/// <summary>A product in a price list, for Marketing: current price and the next scheduled one.</summary>
public record ListPrice(Product Product, decimal? CurrentPrice, decimal? NextPrice, DateTime? NextEffectiveFromUtc);

public record PriceHistoryEntry(decimal Price, DateTime EffectiveFromUtc, string SetBy);

public enum PriceChangeError { NotAllowed, PriceListNotFound, ProductNotFound, InvalidPrice, EffectiveDateInPast }

public record PriceChangeResult(PriceListItem? Item, PriceChangeError? Error)
{
    public bool Succeeded => Item is not null;
}

/// <summary>Price lists per zone. Rules: docs/features/price-lists.md, ADR 0011.</summary>
public class PriceListService(IDbContextFactory<PosDbContext> dbFactory, TimeProvider clock)
{
    /// <summary>
    /// What a branch can sell right now: active products that have a price in the
    /// branch's list, at the latest price already in effect. Sorted by category, then name.
    /// </summary>
    public async Task<List<CatalogEntry>> GetBranchCatalogAsync(Guid branchId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var priceListId = await db.Branches.Where(b => b.Id == branchId).Select(b => b.PriceListId).SingleOrDefaultAsync();
        if (priceListId is null) return [];

        var prices = await CurrentPricesAsync(db, priceListId.Value);
        var products = await db.Products
            .Where(p => p.IsActive && prices.Keys.Contains(p.Id))
            .Include(p => p.Category)
            .Include(p => p.Modifiers.Where(pm => pm.IsActive && pm.Modifier!.IsActive)).ThenInclude(pm => pm.Modifier)
            .AsNoTracking()
            .ToListAsync();

        return products
            .OrderBy(p => p.Category!.Name).ThenBy(p => p.Name)
            .Select(p => new CatalogEntry(p, prices[p.Id]))
            .ToList();
    }

    public async Task<List<PriceList>> GetPriceListsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.PriceLists.Where(l => l.IsActive).OrderBy(l => l.Name).AsNoTracking().ToListAsync();
    }

    /// <summary>Every active product with its current price in the list (if any) and the next scheduled one.</summary>
    public async Task<List<ListPrice>> GetListPricesAsync(Guid priceListId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var current = await CurrentPricesAsync(db, priceListId);
        var now = clock.GetUtcNow().UtcDateTime;
        var upcoming = await db.PriceListItems
            .Where(i => i.PriceListId == priceListId && i.EffectiveFromUtc > now)
            .GroupBy(i => i.ProductId)
            .Select(g => g.OrderBy(i => i.EffectiveFromUtc).First())
            .AsNoTracking()
            .ToDictionaryAsync(i => i.ProductId);
        var products = await db.Products
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync();

        return products.Select(p => new ListPrice(
            p,
            current.TryGetValue(p.Id, out var price) ? price : null,
            upcoming.GetValueOrDefault(p.Id)?.Price,
            upcoming.GetValueOrDefault(p.Id)?.EffectiveFromUtc)).ToList();
    }

    /// <summary>All prices a product has had in a list, newest first, with who set each one.</summary>
    public async Task<List<PriceHistoryEntry>> GetHistoryAsync(Guid priceListId, Guid productId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.PriceListItems
            .Where(i => i.PriceListId == priceListId && i.ProductId == productId)
            .OrderByDescending(i => i.EffectiveFromUtc)
            .Join(db.Users, i => i.SetByUserId, u => u.Id, (i, u) => new PriceHistoryEntry(i.Price, i.EffectiveFromUtc, u.Name))
            .ToListAsync();
    }

    /// <summary>
    /// Sets a product's price in a list, now or from a future moment. Only an active
    /// Marketing user may do it; the role is read from the database, never trusted from
    /// the caller. Adds a row (history is never overwritten) and writes PRICE_CHANGED.
    /// </summary>
    public async Task<PriceChangeResult> SetPriceAsync(Guid actorUserId, Guid priceListId, Guid productId, decimal price, DateTime? effectiveFromUtc = null)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await using var db = await dbFactory.CreateDbContextAsync();

        var actor = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == actorUserId);
        if (actor is not { IsActive: true, Role: UserRole.Marketing }) return new(null, PriceChangeError.NotAllowed);

        var priceList = await db.PriceLists.AsNoTracking().SingleOrDefaultAsync(l => l.Id == priceListId && l.IsActive);
        if (priceList is null) return new(null, PriceChangeError.PriceListNotFound);

        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == productId);
        if (product is null) return new(null, PriceChangeError.ProductNotFound);

        if (price <= 0 || price != Math.Round(price, 2)) return new(null, PriceChangeError.InvalidPrice);

        var effectiveFrom = effectiveFromUtc ?? now;
        if (effectiveFrom < now) return new(null, PriceChangeError.EffectiveDateInPast);

        var hadPrice = (await CurrentPricesAsync(db, priceListId)).TryGetValue(productId, out var previous);
        var item = new PriceListItem
        {
            PriceListId = priceListId, ProductId = productId, Price = price,
            EffectiveFromUtc = effectiveFrom, SetByUserId = actor.Id,
        };
        db.PriceListItems.Add(item);
        db.AuditLogs.Add(new AuditLog
        {
            UserId = actor.Id,
            BranchId = actor.BranchId,
            Action = "PRICE_CHANGED",
            OldValue = hadPrice ? Describe(priceList, product, previous, null) : null,
            NewValue = Describe(priceList, product, price, effectiveFrom),
            CreatedAtUtc = now,
        });
        await db.SaveChangesAsync();
        return new(item, null);
    }

    /// <summary>Product id → the latest price already in effect in the list.</summary>
    private async Task<Dictionary<Guid, decimal>> CurrentPricesAsync(PosDbContext db, Guid priceListId)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return await db.PriceListItems
            .Where(i => i.PriceListId == priceListId && i.EffectiveFromUtc <= now)
            .GroupBy(i => i.ProductId)
            .Select(g => g.OrderByDescending(i => i.EffectiveFromUtc).First())
            .ToDictionaryAsync(i => i.ProductId, i => i.Price);
    }

    private static string Describe(PriceList list, Product product, decimal price, DateTime? effectiveFromUtc) =>
        JsonSerializer.Serialize(new { priceList = list.Name, product = product.Name, price, effectiveFromUtc });
}
