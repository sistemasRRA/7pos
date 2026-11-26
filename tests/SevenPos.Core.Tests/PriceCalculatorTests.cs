using SevenPos.Core.Catalog;

namespace SevenPos.Core.Tests;

public class PriceCalculatorTests
{
    private static readonly Modifier Starch = new() { Name = "Almidón", ChargeType = ChargeType.Fixed, ChargeValue = 10, SortOrder = 1 };
    private static readonly Modifier Express = new() { Name = "Express", ChargeType = ChargeType.Percentage, ChargeValue = 50, SortOrder = 100 };

    [Fact]
    public void BriefExample_Shirt50_Starch_Express_Is90()
    {
        var r = PriceCalculator.Calculate(50, [Starch, Express]);
        Assert.Equal(90m, r.Total);
        Assert.Equal(10m, r.Charges[0].Amount);
        Assert.Equal(30m, r.Charges[1].Amount);
    }

    [Fact]
    public void OrderComesFromSortOrder_NotSelectionOrder()
    {
        Assert.Equal(90m, PriceCalculator.Calculate(50, [Express, Starch]).Total);
    }

    [Fact]
    public void NoModifiers_IsBasePrice()
    {
        Assert.Equal(65m, PriceCalculator.Calculate(65, []).Total);
    }

    [Fact]
    public void Percentage_IsRoundedToCents()
    {
        var m = new Modifier { Name = "X", ChargeType = ChargeType.Percentage, ChargeValue = 15, SortOrder = 1 };
        Assert.Equal(7.65m, PriceCalculator.Calculate(50.99m, [m]).Charges[0].Amount);
    }
}
