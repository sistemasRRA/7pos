# Roadmap

## Phases (from ER revision 2)

| Phase | Scope |
|---|---|
| **1 · Web MVP** | Web app on a central database: catalog and price lists, tickets, payments, shifts, audit, promotions, loyalty |
| 2 · Hybrid and logistics | Windows desktop POS with offline SQLite and sync; CleanCom logistics; back-of-house (damage map, photos, supplies) |
| 3 · Customer experience | Guest portal (PWA), subscriptions, wallet balance, payment links, franchise clearing |
| 4 · IoT | 7Wash machines, kiosks, smart lockers |

## Phase 1, split into deliverable steps

Phase 1 as specified is large for one developer, so it is split:

**1a · Core POS**
1. ✅ Move to PostgreSQL and add the missing foreign keys ([ADR 0005](decisions/0005-postgresql-for-phase-1.md))
   ✅ Organize Core by business area (`Catalog/`, `Tickets/`, `Cash/`, `Guests/`, `Organization/`)
2. ✅ Users and PIN login ([feature doc](features/pin-login.md))
3. ⬜ Catalog with price lists per zone
4. ✅ Guests: search by phone and register ([feature doc](features/guests.md))
5. ⬜ Ticket capture wizard (one row per garment)
6. ⬜ Payments: deposits and settlements
7. ⬜ Shift open / close (Corte X/Z), cash withdrawals, blind count
8. ⬜ Garment lifecycle with status history

**1b · Promotions and coupons:** campaigns, rule-based promotions, coupons, ticket discounts.

**1c · Smart Loyalty:** points ledger, earn and redeem rules, WhatsApp OTP.

Done so far: solution structure, entities, pricing calculator with tests, demo catalog page, generated database diagram, documentation.

## Known issues
None open.

## Open questions for the client
1. **Taxes:** prices include IVA? 16% everywhere, or 8% in the border zone? Do guests need CFDI invoices (RFC, tax regime fields)?
2. **Cash withdrawals:** the brief requires them in the shift close, but the ER has no table. Confirm the fields: reason, authorizer, signature.
3. **Garment status history:** the production report (pieces per operator per day) needs who and when for each status change. Can we add the table?
4. **Modifier order:** confirm that percentage modifiers (Express) apply on top of fixed ones.
5. **Missing price:** if a product has no price in the branch's list, should the sale use the base price or be blocked?
6. **ID types:** ER rev. 2 mixes integer and UUID keys, and the garment uses a text code as its key. We use UUIDs everywhere. Is that acceptable?
7. **Several PCs per branch:** affects how folios are generated, especially offline in Phase 2.
8. **Offline points and wallet (Phase 2):** can points be spent offline? If yes, how do we prevent double spending?
9. **Hash chain scope** for the audit log and ledger: per branch, per guest, or global?
10. **Phase 1 scope:** is splitting it into 1a / 1b / 1c acceptable?
