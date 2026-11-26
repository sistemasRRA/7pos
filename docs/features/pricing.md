# Pricing

## What it does
Each garment starts from a **base price** and the cashier adds **modifiers** (paid extras) one at a time. This is the "Starbucks model" from the client brief: a small catalog (about 60 items), with revenue raised through upsells.

## Rules
1. The price starts at `Product.BasePrice`.
2. Only the modifiers linked to the product (`ProductModifier`) are offered.
3. Modifiers are applied in ascending `Modifier.SortOrder`, **regardless of the order the cashier selected them**.
4. A `Fixed` modifier adds its `ChargeValue` in pesos.
5. A `Percentage` modifier adds `ChargeValue`% of the **running subtotal at that step**. It therefore includes any fixed extras applied before it.
6. Each percentage charge is rounded to cents (2 decimals, half away from zero).
7. When the garment is sold, the price and each charge are copied to `Garment.PriceCharged` and `GarmentModifier.ChargeApplied`, and never recalculated (see [data rules](../data-rules.md#3-freeze-prices-at-the-moment-of-sale)).

**Example (from the brief):** Camisa de vestir $50 → Almidón +$10 = $60 → Servicio Express +50% = **$90**.

Express has `SortOrder = 100` so it always applies last, on top of every other extra.

## Edge cases
- No modifiers selected: the total is the base price.
- Two percentage modifiers: the second applies on top of the first (compounding). Give them distinct `SortOrder` values on purpose.

## Code
- Rule: [`PriceCalculator`](../../src/SevenPos.Core/Catalog/PriceCalculator.cs)
- Tests: [`PriceCalculatorTests`](../../tests/SevenPos.Core.Tests/PriceCalculatorTests.cs)
- Demo screen: [`Catalog.razor`](../../src/SevenPos.UI/Pages/Catalog.razor)

## Coming next (ER revision 2)
The full evaluation order is: list price from the zone's price list, then modifiers, item promotions, ticket promotions, coupon, points redemption, and manual discount (supervisor PIN). Only the first two steps exist today.

## Open questions
- If a product has no price in the branch's price list, is the base price used, or is the sale blocked?
- Is IVA included in the displayed prices? (Assumed yes. Not modeled yet.)
