using SevenPos.Core.Catalog;

namespace SevenPos.Core.Tests;

/// <summary>Rules in docs/features/pricing.md and ADR 0012.</summary>
public class PriceCalculatorTests
{
    private static Modifier Percent(string name, decimal percent, int sortOrder) =>
        new() { Name = name, ChargeType = ChargeType.Percentage, ChargeValue = percent, SortOrder = sortOrder };

    private static readonly Modifier Starch = Percent("Almidón medio", 10, 6);
    private static readonly Modifier Express = Percent("Urgente", 50, 8);
    private static readonly Modifier FixedExtra = new() { Name = "Fijo", ChargeType = ChargeType.Fixed, ChargeValue = 10, SortOrder = 1 };

    [Fact]
    public void ClientExample_Shirt100_Starch10Percent_Express_Is160()
    {
        // "no va calcular el servicio exprés sobre los 110 … sino sobre los 100": 100 + 10 + 50.
        var r = PriceCalculator.Calculate(100, [Starch, Express]);

        Assert.Equal(160m, r.Total);
        Assert.Equal([10m, 50m], r.Charges.Select(c => c.Amount));
    }

    [Fact]
    public void Percentage_IsOfTheListPrice_NotOfOtherExtras()
    {
        // A fixed extra does not raise the base of a percentage either: 50 + 10 + 25.
        Assert.Equal(85m, PriceCalculator.Calculate(50, [FixedExtra, Express]).Total);
    }

    [Fact]
    public void SelectionOrder_DoesNotChangeTheTotal_ChargesAreListedInSortOrder()
    {
        var r = PriceCalculator.Calculate(100, [Express, Starch]);

        Assert.Equal(160m, r.Total);
        Assert.Equal(["Almidón medio", "Urgente"], r.Charges.Select(c => c.Modifier.Name));
    }

    [Fact]
    public void NoModifiers_IsBasePrice()
    {
        Assert.Equal(65m, PriceCalculator.Calculate(65, []).Total);
    }

    [Fact]
    public void Percentage_IsRoundedToCents()
    {
        Assert.Equal(7.65m, PriceCalculator.Calculate(50.99m, [Percent("X", 15, 1)]).Charges[0].Amount);
    }
}
