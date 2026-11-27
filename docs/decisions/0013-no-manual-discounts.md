# 0013 · No manual discounts

- **Status:** Accepted
- **Date:** 2026-11-26

## Context
ER revision 2 ends its price evaluation with a "manual discount (supervisor PIN)". The client answered that discounts happen only when there is a promotion; staff cannot give a discount manually ([answers](../specs/2026-11-26-modifiers-discounts-price-lists.md)). This matches [ADR 0011](0011-only-marketing-controls-catalog-and-prices.md): branch staff cannot lower a price in any way.

## Decision
- The POS has **no manual discount**: no free amount, no percentage, not even with a supervisor PIN.
- A ticket's `Discounts` come only from promotions and coupons configured by Marketing (roadmap 1b), each one recorded so the discount can be explained.
- Until promotions exist, `Discounts = 0` and `Total = Subtotal`.

## Alternatives considered
- **Manual discount with a supervisor PIN** (ER rev. 2): the kind of discretion the client wants to remove.

## Consequences
- The price evaluation order is: list price → modifiers → item promotions → ticket promotions → coupon → points redemption. There is no manual step.
- Unhappy-customer cases must be handled by promotions or coupons Marketing provides, or by the "Política de calidad" $0 items (still to be explained by the client).
