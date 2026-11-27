# 0012 · Percentage modifiers apply to the list price

- **Status:** Accepted
- **Date:** 2026-11-26

## Context
The brief's example (shirt $50 → starch +$10 → Express +50% = $90) suggested that Express applies on top of other extras, so the first version of `PriceCalculator` compounded percentages over a running subtotal, ordered by `SortOrder`. The client corrected it ([answers](../specs/2026-11-26-modifiers-discounts-price-lists.md)): a $100 shirt with 10% starch and 50% Express costs **$160**, because Express is calculated on the $100, not on the $110.

## Decision
- A `Percentage` modifier adds `ChargeValue`% of the garment's **list price**, never of the price with other extras.
- A `Fixed` modifier adds its amount (kept for possible future extras; the 2026 lists have none).
- Charges simply add up: `PriceCharged = ListPrice + sum(charges)`. Each charge is rounded to cents.
- `Modifier.SortOrder` only decides the order the wizard asks and lists the extras. It no longer changes the price.

## Alternatives considered
- **Compounding over a running subtotal** (the first version): contradicts the client's example ($165 instead of $160).
- **Applying Express to the price with all other extras:** same problem.

## Consequences
- The order of the cashier's answers can never change a price, which removes a class of mistakes.
- Tickets created before this change (development only) keep their frozen prices.
