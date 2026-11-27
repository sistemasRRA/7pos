using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using SevenPos.Core.Data;

namespace SevenPos.Core.Catalog;

public enum CatalogImportError { NotAllowed }

/// <summary>What an import added. Rows it could not load are listed in Skipped, with the reason.</summary>
public record CatalogImportResult(int Products, int Groups, int Options, int Prices, IReadOnlyList<string> Skipped, CatalogImportError? Error = null)
{
    public bool Succeeded => Error is null;
}

/// <summary>
/// Loads Marketing's price lists (CSV, as transcribed in docs/specs) into the catalog:
/// service lines, modifier groups and options, categories, products, which options each
/// product offers, and one price per list. Running it again only adds what is missing or
/// changed. Rules: docs/features/real-catalog.md.
/// </summary>
public class CatalogImporter(IDbContextFactory<PosDbContext> dbFactory, TimeProvider clock)
{
    /// <summary>
    /// Lines whose 2026 data is clear enough to sell. Sastrería, 7shoes and Lavandería wait
    /// for the client's answers (docs/specs/2026-11-26-modifier-groups-and-full-list-a.md).
    /// </summary>
    public static readonly IReadOnlyList<string> ReadyServiceLines2026 = ["Tintorería", "Planchado", "7PLUS"];

    /// <summary>Client: only bed items offer the bed sizes.</summary>
    public const string BedCategory = "Ropa de cama y blancos";
    public static readonly IReadOnlySet<string> BedSizes = new HashSet<string> { "Matrimonial", "Queen / King" };

    private static readonly (string Column, string ListName)[] ListColumns =
        [("list_a", "Lista A"), ("list_aa", "Lista AA"), ("list_b", "Lista B"), ("list_c", "Lista C")];

    /// <summary>Loads the 2026 lists embedded in this assembly, for the ready service lines.</summary>
    public async Task<CatalogImportResult> Import2026Async(Guid actorUserId)
    {
        using var products = OpenResource("catalog-2026-products.csv");
        using var modifiers = OpenResource("catalog-2026-modifiers.csv");
        return await ImportAsync(actorUserId, products, modifiers, ReadyServiceLines2026);
    }

    public async Task<CatalogImportResult> ImportAsync(Guid actorUserId, TextReader products, TextReader modifiers, IReadOnlyList<string> serviceLines)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var actor = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == actorUserId);
        if (actor is not { IsActive: true, Role: UserRole.Marketing }) return new(0, 0, 0, 0, [], CatalogImportError.NotAllowed);

        var now = clock.GetUtcNow().UtcDateTime;
        int newProducts = 0, newGroups = 0, newOptions = 0, newPrices = 0;
        var skipped = new List<string>();

        // Service lines, in the order requested.
        var lines = new Dictionary<string, ServiceLine>();
        for (var i = 0; i < serviceLines.Count; i++)
        {
            var name = serviceLines[i];
            var line = await db.ServiceLines.SingleOrDefaultAsync(l => l.Name == name);
            if (line is null) db.ServiceLines.Add(line = new ServiceLine { Name = name, SortOrder = i + 1 });
            lines[name] = line;
        }
        var lineIds = lines.Values.Select(l => l.Id).ToList();

        // Groups (with a step number) and their options.
        var groups = await db.ModifierGroups.Include(g => g.Modifiers)
            .Where(g => lineIds.Contains(g.ServiceLineId)).ToListAsync();
        foreach (var row in ReadCsv(modifiers).Where(r => lines.ContainsKey(r["service"])))
        {
            if (!int.TryParse(row["step"], out var step))
            {
                skipped.Add($"{row["service"]} · {row["group"]}: {row["option"]}: no step number");
                continue;
            }
            var line = lines[row["service"]];
            var group = groups.SingleOrDefault(g => g.ServiceLineId == line.Id && g.Name == row["group"]);
            if (group is null)
            {
                groups.Add(group = new ModifierGroup { ServiceLineId = line.Id, Name = row["group"], SortOrder = step });
                db.ModifierGroups.Add(group);
                newGroups++;
            }
            if (group.Modifiers.Any(m => m.Name == row["option"])) continue;
            group.Modifiers.Add(new Modifier
            {
                Name = row["option"],
                ChargeType = row["charge_type"] == "fixed" ? ChargeType.Fixed : ChargeType.Percentage,
                ChargeValue = decimal.Parse(row["value"], CultureInfo.InvariantCulture),
                SortOrder = group.Modifiers.Count + 1,
            });
            newOptions++;
        }

        // Products, their options and their prices.
        var categories = await db.Categories.ToDictionaryAsync(c => c.Name);
        var existing = await db.Products.Include(p => p.Modifiers)
            .Where(p => lineIds.Contains(p.ServiceLineId)).ToListAsync();
        var lists = await db.PriceLists.ToDictionaryAsync(l => l.Name);
        var currentPrices = await CurrentPricesAsync(db, now);
        var seen = new HashSet<(string Line, string Product)>();

        foreach (var row in ReadCsv(products).Where(r => lines.ContainsKey(r["service"])))
        {
            var line = lines[row["service"]];
            var name = row["product"];
            var prices = ListColumns
                .Where(c => row[c.Column] != "")
                .Select(c => (c.ListName, Price: decimal.Parse(row[c.Column], CultureInfo.InvariantCulture)))
                .ToList();

            if (!seen.Add((line.Name, name)))
            {
                skipped.Add($"{line.Name} · {name} (${prices.FirstOrDefault().Price:0}): repeated product");
                continue;
            }
            if (prices.Count == 0 || prices.Any(p => p.Price <= 0))
            {
                skipped.Add($"{line.Name} · {name}: price $0");
                continue;
            }

            if (!categories.TryGetValue(row["group"], out var category))
                db.Categories.Add(categories[row["group"]] = category = new Category { Name = row["group"] });

            var product = existing.SingleOrDefault(p => p.ServiceLineId == line.Id && p.Name == name);
            if (product is null)
            {
                product = new Product { ServiceLineId = line.Id, CategoryId = category.Id, Name = name, BasePrice = prices[0].Price };
                existing.Add(product);
                db.Products.Add(product);
                newProducts++;
            }

            var isBedItem = category.Name == BedCategory;
            foreach (var option in groups.Where(g => g.ServiceLineId == line.Id).SelectMany(g => g.Modifiers))
            {
                if (BedSizes.Contains(option.Name) && !isBedItem) continue;
                if (product.Modifiers.Any(pm => pm.ModifierId == option.Id)) continue;
                product.Modifiers.Add(new ProductModifier { ModifierId = option.Id });
            }

            foreach (var (listName, price) in prices)
            {
                if (!lists.TryGetValue(listName, out var list))
                    db.PriceLists.Add(lists[listName] = list = new PriceList { Name = listName });
                if (currentPrices.TryGetValue((list.Id, product.Id), out var current) && current == price) continue;
                db.PriceListItems.Add(new PriceListItem
                {
                    PriceListId = list.Id, ProductId = product.Id, Price = price,
                    EffectiveFromUtc = now, SetByUserId = actor.Id,
                });
                newPrices++;
            }
        }

        db.AuditLogs.Add(new AuditLog
        {
            UserId = actor.Id,
            BranchId = actor.BranchId,
            Action = "CATALOG_IMPORTED",
            NewValue = JsonSerializer.Serialize(new
            {
                serviceLines, products = newProducts, groups = newGroups, options = newOptions, prices = newPrices, skipped,
            }),
            CreatedAtUtc = now,
        });
        await db.SaveChangesAsync();
        return new(newProducts, newGroups, newOptions, newPrices, skipped);
    }

    /// <summary>(list, product) → the price in effect now.</summary>
    private static async Task<Dictionary<(Guid, Guid), decimal>> CurrentPricesAsync(PosDbContext db, DateTime now)
    {
        var items = await db.PriceListItems.Where(i => i.EffectiveFromUtc <= now)
            .Select(i => new { i.PriceListId, i.ProductId, i.Price, i.EffectiveFromUtc })
            .ToListAsync();
        return items
            .GroupBy(i => (i.PriceListId, i.ProductId))
            .ToDictionary(g => g.Key, g => g.MaxBy(i => i.EffectiveFromUtc)!.Price);
    }

    /// <summary>Reads a CSV with a header row; quoted fields may contain commas.</summary>
    private static IEnumerable<Dictionary<string, string>> ReadCsv(TextReader reader)
    {
        using var parser = new TextFieldParser(reader) { HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = true };
        parser.SetDelimiters(",");
        var header = parser.ReadFields() ?? [];
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || fields.All(f => f == "")) continue;
            yield return header.Select((h, i) => (h, Value: i < fields.Length ? fields[i] : "")).ToDictionary(x => x.h, x => x.Value);
        }
    }

    private static StreamReader OpenResource(string name) => new(
        typeof(CatalogImporter).Assembly.GetManifestResourceStream(name)
        ?? throw new InvalidOperationException($"Embedded resource {name} not found."));
}
