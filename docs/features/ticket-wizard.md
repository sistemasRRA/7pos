# Ticket capture wizard

## What it does
At the counter, the cashier creates a ticket for a customer in three steps: **Cliente** (search or register), **Prendas** (each garment and its extras, asked one at a time, the "Starbucks model"), and **Confirmar**. The ticket is stored with **one row per physical garment**, each with its own label code, and every price is frozen at the moment of sale. Payment, printing and photos come in later steps.

## Rules

**Who and for whom** (`TicketService.CreateAsync`)
1. Any **active** branch user can create tickets, **except Marketing**. Unknown users are refused too.
2. The ticket belongs to the **cashier's branch** and to an **active guest**.
3. A ticket needs **at least one garment**.

**Prices** ([pricing](pricing.md), [price lists](price-lists.md))
4. Every price is read again from the **branch's price list at that moment**, never taken from the screen ([ADR 0011](../decisions/0011-only-marketing-controls-catalog-and-prices.md)). The screen only shows a preview.
5. A product without a price in the branch list, or an inactive one, is refused.
6. A modifier the product does not offer (or an inactive one) is refused. The same modifier sent twice is charged once.
7. A percentage extra is a percentage of the **list price**, never of the price with other extras: $100 + Almidón 10% + Express 50% = $160 ([ADR 0012](../decisions/0012-percentage-modifiers-on-list-price.md)). Charges are stored in `Modifier.SortOrder`.
8. Each garment stores its list price (`ListPrice`), its final price (`PriceCharged`) and each extra's charge (`GarmentModifier.ChargeApplied`). Later price changes never change a saved ticket ([data rules](../data-rules.md#3-freeze-prices-at-the-moment-of-sale)).
9. `Subtotal` = sum of the garment prices; `Discounts` = 0 until promotions exist, because there are **no manual discounts** ([ADR 0013](../decisions/0013-no-manual-discounts.md)); `Total = Subtotal − Discounts`; `BalanceDue = Total` until payments exist.
10. Prices include IVA. The screen breaks the ticket total down, per ticket ([ADR 0010](../decisions/0010-prices-include-iva.md)).

**Numbers**
11. **Folio:** sequential per branch, starting at 1. If two terminals take the same folio at once, the unique index `(branch_id, folio)` refuses one and the service retries with the next number.
12. **Public order number:** 8 random characters without look-alikes (no 0/O, 1/I/L), unique. It is what the customer receives; the folio stays internal.
13. **Garment label** (marcador): `BRANCH-YYYYMMDD-FOLIO-SEQ`, e.g. `DEMO-20261126-000042-03`. The date is the **Mexico City** calendar day, so a ticket at 21:00 is not labeled with the next day.

**Records**
14. The guest's `LastVisitUtc` is set to the ticket time.
15. Each ticket is written to the audit log as `TICKET_CREATED` with ticket ID, folio, total and number of garments.
16. A refused ticket saves nothing.

## Screen
- `/tickets/new`, header link "Nuevo ticket" (hidden for Marketing).
- In Prendas, the cashier picks the service line and the garment, then answers each of its modifier groups in step order (1 Tamaño, 2 Textil…), one option or "Ninguno" each, then the quantity ([real catalog](real-catalog.md)). "3 × CAMISA, Almidón Medio" becomes 3 garment rows with 3 labels.
- The result screen shows the folio, the public order number, each label code and the balance due.

## Edge cases
- **A price changes while the cashier is capturing** (a scheduled price starts): the saved ticket uses the price at the moment it is created, and the result screen shows the saved prices.
- **A product or extra is turned off meanwhile:** the ticket is refused with a message and the catalog reloads.
- **Existing branches** got placeholder codes `S001`, `S002`… in the migration. Real codes must be set before labels are printed.
- **Shift:** the ticket belongs to the branch's open shift. Without an open shift `ShiftId` stays empty for now; payments will require one ([shifts](shifts.md)).
- **Several PCs offline** (Phase 2) cannot use "highest folio + 1". Folio generation will need a decision then (roadmap question 7).

## Code
- Rules: [`TicketService`](../../src/SevenPos.Core/Tickets/TicketService.cs), [`BusinessTime`](../../src/SevenPos.Core/Common/BusinessTime.cs)
- Model: [`Ticket`](../../src/SevenPos.Core/Tickets/Ticket.cs), [`Garment`](../../src/SevenPos.Core/Tickets/Garment.cs) (`ListPrice`), [`GarmentModifier`](../../src/SevenPos.Core/Tickets/GarmentModifier.cs), `Branch.Code`
- Tests: [`TicketServiceTests`](../../tests/SevenPos.Core.Tests/TicketServiceTests.cs), [`BusinessTimeTests`](../../tests/SevenPos.Core.Tests/BusinessTimeTests.cs)
- Screen: [`TicketWizard.razor`](../../src/SevenPos.UI/Tickets/TicketWizard.razor), components [`GuestPicker`](../../src/SevenPos.UI/Guests/GuestPicker.razor), [`LineList`](../../src/SevenPos.UI/Tickets/LineList.razor), [`Totals`](../../src/SevenPos.UI/Tickets/Totals.razor)

## Not built yet
- Promotions and coupons (roadmap 1b), the only source of discounts.
- Modifier groups and the real 2026 price lists A, AA, B and C ([client answers](../specs/2026-11-26-modifiers-discounts-price-lists.md)).
- Payment at the end of the wizard (roadmap step 6), printing the ticket and labels, the photo step (Phase 2).
- Searching and opening existing tickets; cancelling a ticket.
- Editing branch codes.

## Open questions
- Is a promised delivery date captured on the ticket?
- IVA breakdown per ticket (current) or per garment? They can differ by cents.
