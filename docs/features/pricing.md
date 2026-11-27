# Pricing

## What it does
Each garment starts from a **base price** and the cashier adds **modifiers** (paid extras) one at a time. This is the "Starbucks model" from the client brief: a small catalog (about 60 items), with revenue raised through upsells.

## Rules
1. The price starts at the product's price in the branch's price list (see [price lists](price-lists.md)). `Product.BasePrice` is only a reference.
2. Only the modifiers linked to the product (`ProductModifier`) are offered.
3. A `Percentage` modifier adds `ChargeValue`% of the garment's **list price**, never of the price with other extras ([ADR 0012](../decisions/0012-percentage-modifiers-on-list-price.md)). Charges simply add up.
4. A `Fixed` modifier adds its `ChargeValue` in pesos.
5. Charges are listed in ascending `Modifier.SortOrder`. The order never changes the price.
6. Each percentage charge is rounded to cents (2 decimals, half away from zero).
7. When the garment is sold ([ticket wizard](ticket-wizard.md)), the list price, the final price and each charge are copied to `Garment.ListPrice`, `Garment.PriceCharged` and `GarmentModifier.ChargeApplied`, and never recalculated (see [data rules](../data-rules.md#3-freeze-prices-at-the-moment-of-sale)).

**Example (from the client):** Camisa $100 → Almidón 10% = +$10 → Express 50% = +$50 (50% of $100, not of $110) → **$160**.

## Edge cases
- No modifiers selected: the total is the list price.
- Two percentage modifiers never compound: 10% + 50% is 60% of the list price.

## Code
- Rule: [`PriceCalculator`](../../src/SevenPos.Core/Catalog/PriceCalculator.cs)
- Tests: [`PriceCalculatorTests`](../../tests/SevenPos.Core.Tests/PriceCalculatorTests.cs)
- Demo screen: [`Catalog.razor`](../../src/SevenPos.UI/Pages/Catalog.razor)

## Coming next (ER revision 2)
The full evaluation order is: list price from the zone's price list, then modifiers, item promotions, ticket promotions, coupon, and points redemption. There is **no manual discount** ([ADR 0013](../decisions/0013-no-manual-discounts.md)). Only the first two steps exist today.

The 2026 price lists organize extras in **groups with options** (Tamaño, Textil, Almidón…), all percentages; see the [client answers](../specs/2026-11-26-modifiers-discounts-price-lists.md). Supporting them is the next feature.

## Decided
- A product without a price in the branch's price list is not sold. Only Marketing sets prices ([ADR 0011](../decisions/0011-only-marketing-controls-catalog-and-prices.md)).
- All prices include IVA. Tickets break it down from the total and never add it ([ADR 0010](../decisions/0010-prices-include-iva.md)).

## Open questions
- See the follow-ups on modifier groups in the [client answers](../specs/2026-11-26-modifiers-discounts-price-lists.md#3-the-2026-price-lists).
