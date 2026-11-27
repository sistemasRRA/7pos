using SevenPos.Core.Catalog;

namespace SevenPos.Core.Tests;

/// <summary>Unit tests for ADR 0010: prices include IVA, the system only breaks it down.</summary>
public class TaxCalculatorTests
{
    [Fact]
    public void ClientExample_100_Is_86_21_Plus_13_79()
    {
        var b = TaxCalculator.Breakdown(100m);

        Assert.Equal(86.21m, b.Subtotal);
        Assert.Equal(13.79m, b.Iva);
        Assert.Equal(100m, b.Total);
    }

    [Theory]
    [InlineData(50, 43.10, 6.90)]
    [InlineData(90, 77.59, 12.41)]
    [InlineData(116, 100.00, 16.00)]
    [InlineData(0.01, 0.01, 0.00)]
    [InlineData(0, 0, 0)]
    public void KnownAmounts(decimal total, decimal subtotal, decimal iva)
    {
        var b = TaxCalculator.Breakdown(total);

        Assert.Equal(subtotal, b.Subtotal);
        Assert.Equal(iva, b.Iva);
    }

    [Fact]
    public void SubtotalPlusIva_AlwaysEqualsTotal_ToTheCent()
    {
        for (var cents = 1; cents <= 200_000; cents++)
        {
            var total = cents / 100m;
            var b = TaxCalculator.Breakdown(total);
            Assert.True(b.Subtotal + b.Iva == total, $"{total}: {b.Subtotal} + {b.Iva}");
        }
    }

    [Fact]
    public void NegativeTotal_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TaxCalculator.Breakdown(-1m));
    }

    [Fact]
    public void TotalWithFractionsOfACent_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => TaxCalculator.Breakdown(10.005m));
    }
}
