# 0010 · Prices include IVA; the system only breaks it down

- **Status:** Accepted. The exact calculation is pending client confirmation.
- **Date:** 2026-11-26

## Context
The client confirmed ([answers 2026-11-26](../specs/2026-11-26-client-answers.md)) that every price shown to customers already includes 16% IVA: "NO SUMAMOS, DESGLOSAMOS". Tickets and reports must show the subtotal and the IVA contained in the total.

## Decision
- Every price stored in the system (base prices, price lists, modifiers, `Garment.PriceCharged`, ticket totals) is **final, IVA included**.
- The breakdown is calculated from the total, never added to it:
  ```
  Subtotal = round(Total / 1.16, 2)
  IVA      = Total − Subtotal
  ```
  Deriving IVA as the difference guarantees Subtotal + IVA = Total to the cent. The client's own formula (`IVA = Subtotal × 0.16`) can be off by a cent for some amounts.
- The breakdown is done **once per ticket**, on the final total after discounts, not per garment.
- The IVA rate is a single value in code (16%) until the client confirms whether any branch uses the 8% border rate. If one does, the rate becomes a per-branch setting.

## Alternatives considered
- **Store prices before IVA and add it at checkout:** contradicts how the business sets and advertises prices, and the published price is what binds them (PROFECO).
- **Break down per garment and add up:** the per-garment subtotals can disagree with the ticket total by cents.

## Consequences
- No IVA column is needed on products or price lists.
- Tickets store the total and its breakdown (subtotal and IVA) when they are closed, so reports never recalculate history.
- To confirm with the client: the rounding method, per-ticket breakdown, the 8% zone, and invoicing (CFDI).
