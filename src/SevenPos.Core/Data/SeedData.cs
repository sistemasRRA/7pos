using Microsoft.EntityFrameworkCore;

namespace SevenPos.Core.Data;

/// <summary>Sample data for development. Only loaded when the database is empty.</summary>
public static class SeedData
{
    public static async Task LoadAsync(PosDbContext db)
    {
        if (await db.Branches.AnyAsync()) return;

        var branch = new Branch { Name = "Sucursal Demo" };
        db.Branches.Add(branch);
        db.Users.Add(new User
        {
            Name = "Admin", Role = UserRole.Admin,
            PinHash = PinHasher.Hash("1234"), BranchId = branch.Id,
        });

        // Display names stay in Spanish: they are what the cashier sees.
        var starch = new Modifier { Name = "Almidón", ChargeType = ChargeType.Fixed, ChargeValue = 10, SortOrder = 1 };
        var stainRemoval = new Modifier { Name = "Desmanchado profundo", ChargeType = ChargeType.Fixed, ChargeValue = 25, SortOrder = 2 };
        var whiteSole = new Modifier { Name = "Suela blanca", ChargeType = ChargeType.Fixed, ChargeValue = 40, SortOrder = 3 };
        var express = new Modifier { Name = "Servicio Express", ChargeType = ChargeType.Percentage, ChargeValue = 50, SortOrder = 100 };

        Category NewCategory(string name, params (string name, decimal price, Modifier[] modifiers)[] products) => new()
        {
            Name = name,
            Products = products.Select(p => new Product
            {
                Name = p.name, BasePrice = p.price,
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
}
