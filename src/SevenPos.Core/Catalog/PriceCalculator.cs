namespace SevenPos.Core.Catalog;

public record AppliedCharge(Modifier Modifier, decimal Amount);
public record PriceResult(decimal BasePrice, IReadOnlyList<AppliedCharge> Charges, decimal Total);

/// <summary>
/// Incremental pricing ("Starbucks model"): list price + modifiers.
/// A percentage modifier is always a percentage of the list price, never of the
/// price with other extras (ADR 0012), so charges simply add up and the order
/// does not change the total. Charges are listed in Modifier.SortOrder.
/// E.g. Shirt $100 → Almidón 10% = +$10 → Urgente 50% = +$50 → $160.
/// </summary>
public static class PriceCalculator
{
    public static PriceResult Calculate(decimal basePrice, IEnumerable<Modifier> modifiers)
    {
        var charges = modifiers
            .OrderBy(m => m.SortOrder)
            .Select(m => new AppliedCharge(m, m.ChargeType == ChargeType.Fixed
                ? m.ChargeValue
                : Round(basePrice * m.ChargeValue / 100m)))
            .ToList();

        return new PriceResult(basePrice, charges, basePrice + charges.Sum(c => c.Amount));
    }

    public static decimal Round(decimal amount) =>
        Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
