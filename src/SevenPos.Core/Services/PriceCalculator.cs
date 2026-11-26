using SevenPos.Core.Entities;

namespace SevenPos.Core.Services;

public record AppliedCharge(Modifier Modifier, decimal Amount);
public record PriceResult(decimal BasePrice, IReadOnlyList<AppliedCharge> Charges, decimal Total);

/// <summary>
/// Incremental pricing ("Starbucks model"): base price + modifiers.
/// Modifiers are applied by Modifier.SortOrder; a percentage is calculated
/// on the running subtotal at that step.
/// E.g. Shirt $50 → Starch +$10 = $60 → Express +50% = $90.
/// </summary>
public static class PriceCalculator
{
    public static PriceResult Calculate(decimal basePrice, IEnumerable<Modifier> modifiers)
    {
        var subtotal = basePrice;
        var charges = new List<AppliedCharge>();

        foreach (var m in modifiers.OrderBy(m => m.SortOrder))
        {
            var amount = m.ChargeType == ChargeType.Fixed
                ? m.ChargeValue
                : Round(subtotal * m.ChargeValue / 100m);
            charges.Add(new AppliedCharge(m, amount));
            subtotal += amount;
        }

        return new PriceResult(basePrice, charges, subtotal);
    }

    public static decimal Round(decimal amount) =>
        Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
