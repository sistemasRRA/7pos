using Microsoft.EntityFrameworkCore;

namespace SevenPos.Core.Data;

/// <summary>
/// Sample data for development only (never loaded in production, see Program.cs).
/// Each step checks for its own data, so a database created by an older version of
/// the app is completed instead of skipped.
/// </summary>
public static class SeedData
{
    /// <summary>Sample PINs: Admin 1234, Mercadotecnia 5678.</summary>
    public static async Task LoadAsync(PosDbContext db)
    {
        await LoadBranchAndCatalogAsync(db);
        await EnsureMarketingUserAsync(db);
        await EnsureDefaultPriceListAsync(db);
    }

    private static async Task LoadBranchAndCatalogAsync(PosDbContext db)
    {
        if (await db.Branches.AnyAsync()) return;

        var branch = new Branch { Name = "Sucursal Demo", Code = "DEMO" };
        db.Branches.Add(branch);
        db.Users.Add(new User
        {
            Name = "Admin", Role = UserRole.Admin,
            PinHash = PinHasher.Hash("1234"), BranchId = branch.Id,
        });

        // Display names stay in Spanish: they are what the cashier sees.
        var line = new ServiceLine { Name = "General" };
        db.ServiceLines.Add(line);
        Modifier Extra(string name, ChargeType type, decimal value, int step)
        {
            var group = new ModifierGroup { ServiceLineId = line.Id, Name = name, SortOrder = step };
            db.ModifierGroups.Add(group);
            return new Modifier { ModifierGroupId = group.Id, Name = name, ChargeType = type, ChargeValue = value };
        }
        var starch = Extra("Almidón", ChargeType.Fixed, 10, 1);
        var stainRemoval = Extra("Desmanchado profundo", ChargeType.Fixed, 25, 2);
        var whiteSole = Extra("Suela blanca", ChargeType.Fixed, 40, 3);
        var express = Extra("Servicio Express", ChargeType.Percentage, 50, 100);

        Category NewCategory(string name, params (string name, decimal price, Modifier[] modifiers)[] products) => new()
        {
            Name = name,
            Products = products.Select(p => new Product
            {
                ServiceLineId = line.Id, Name = p.name, BasePrice = p.price,
                Modifiers = p.modifiers.Select(m => new ProductModifier { ModifierId = m.Id }).ToList(),
            }).ToList(),
        };

        db.Modifiers.AddRange(starch, stainRemoval, whiteSole, express);
        db.Categories.AddRange(
            NewCategory("Camisas",
                ("Camisa de vestir", 50, [starch, stainRemoval, express]),
                ("Blusa", 55, [starch, stainRemoval, express])),
            NewCategory("Pantalones",
                ("Pantalón de vestir", 65, [starch, stainRemoval, express]),
                ("Jeans", 60, [stainRemoval, express])),
            NewCategory("Blancos",
                ("Edredón matrimonial", 180, [stainRemoval, express]),
                ("Cobija", 150, [express])),
            NewCategory("Tenis",
                ("Tenis", 120, [whiteSole, stainRemoval, express])));

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

    /// <summary>A "Lista General" with every product at its base price, used by every branch without a list.</summary>
    private static async Task EnsureDefaultPriceListAsync(PosDbContext db)
    {
        if (await db.PriceLists.AnyAsync()) return;
        var marketing = await db.Users.FirstAsync(u => u.Role == UserRole.Marketing);
        var list = new PriceList { Name = "Lista General" };
        var now = DateTime.UtcNow;
        list.Items = await db.Products
            .Where(p => p.IsActive)
            .Select(p => new PriceListItem { ProductId = p.Id, Price = p.BasePrice, EffectiveFromUtc = now, SetByUserId = marketing.Id })
            .ToListAsync();
        db.PriceLists.Add(list);

        foreach (var branch in await db.Branches.Where(b => b.PriceListId == null).ToListAsync())
            branch.PriceListId = list.Id;
        await db.SaveChangesAsync();
    }
}
