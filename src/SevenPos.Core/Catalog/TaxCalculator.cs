namespace SevenPos.Core.Catalog;

/// <summary>The IVA contained in a price that already includes it.</summary>
public record TaxBreakdown(decimal Subtotal, decimal Iva, decimal Total);

/// <summary>
/// Prices already include IVA; the system only breaks it down, never adds it.
/// See docs/decisions/0010-prices-include-iva.md.
/// </summary>
public static class TaxCalculator
{
    public const decimal IvaRate = 0.16m;

    /// <summary>
    /// Subtotal = round(Total / 1.16, 2); IVA = Total − Subtotal. Deriving IVA as the
    /// difference guarantees Subtotal + IVA = Total to the cent.
    /// </summary>
    public static TaxBreakdown Breakdown(decimal total)
    {
        if (total < 0) throw new ArgumentOutOfRangeException(nameof(total), total, "A total cannot be negative.");
        if (total != Math.Round(total, 2)) throw new ArgumentException($"A total must be whole cents: {total}.", nameof(total));

        var subtotal = PriceCalculator.Round(total / (1 + IvaRate));
        return new TaxBreakdown(subtotal, total - subtotal, total);
    }
}
