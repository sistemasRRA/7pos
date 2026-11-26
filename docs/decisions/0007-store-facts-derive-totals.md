# 0007 · Store facts, derive totals

- **Status:** Accepted
- **Date:** 2026-11-12

## Context
The spec stored running totals next to the facts they summarize: shift cash and card totals in `Cajas_Turnos`, and wallet balance in `Invitados` as well as in `WALLET_LEDGER`. Two copies of a number eventually disagree, especially with offline branches.

## Decision
- Store each event as its own row: a payment, a cash withdrawal, a points movement, a garment status change.
- Totals and balances are **calculated** from those rows.
- If a total must be frozen (e.g. the printed Corte Z), store it explicitly as a **snapshot taken at close**, labeled as such.
- Each `Payment` carries its `ShiftId` so shift totals are a simple sum.

## Alternatives considered
- **Running counters updated on each change:** fast to read, but one missed update or failed sync makes them wrong forever, with no way to know which copy is right.

## Consequences
- Totals are always explainable: every peso can be traced to a row.
- Reports run aggregate queries, which need indexes on the grouping columns (e.g. `Payment.ShiftId`).
- Planned tables follow this pattern: garment status history and the points ledger.
