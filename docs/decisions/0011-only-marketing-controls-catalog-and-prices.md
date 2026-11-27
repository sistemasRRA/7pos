# 0011 · Only Marketing controls the catalog and prices

- **Status:** Accepted
- **Date:** 2026-11-26

## Context
The client reported that branch staff abuse the ability to create items and change prices ([answers 2026-11-26](../specs/2026-11-26-client-answers.md)). The brief already says prices and modifier rules are "dictated and authorized exclusively" by Marketing in the Web Admin.

## Decision
- Only users with a **Marketing** role (to be added to `UserRole`) can create or deactivate products, modifiers and price lists, and set prices.
- Branch users (cashier, manager) **cannot** create items, type a price, or override a price at the counter. The POS has no free-price field.
- A branch sells only the products that have a price in **its** price list. A product without a price there is simply not shown.
- If the customer brings something not in the catalog, the cashier picks an item of the same or similar price and reports it to their supervisor.
- Every catalog and price change is written to the audit log.

## Alternatives considered
- **Let managers adjust prices with a supervisor PIN:** this is exactly the abuse the client wants to stop.
- **Fall back to the product's base price when the branch list has none:** would sell items Marketing did not authorize for that zone.

## Consequences
- `UserRole` needs a `Marketing` value, and the catalog screens must check the role in the service layer, not only in the UI.
- Manual discounts are a separate decision, still to be confirmed with the client. Until then, the POS offers none.
- Branches depend on Marketing to add missing items quickly. A "report missing item" flow may be worth adding later.

## Update (2026-11-26)
The client answered the manual discounts question: there are none. See [ADR 0013](0013-no-manual-discounts.md).
