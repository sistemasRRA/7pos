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
3. ✅ Catalog with price lists per zone ([feature doc](features/price-lists.md))
4. ✅ Guests: search by phone and register ([feature doc](features/guests.md))
5. ✅ Ticket capture wizard, one row per garment ([feature doc](features/ticket-wizard.md)). Manual discounts wait for the client.
6. ⬜ Payments: deposits and settlements
7. ⬜ Shift open / close (Corte X/Z), cash withdrawals, blind count
8. ⬜ Garment lifecycle with status history

**1b · Promotions and coupons:** campaigns, rule-based promotions, coupons, ticket discounts.

**1c · Smart Loyalty:** points ledger, earn and redeem rules, WhatsApp OTP.

Done so far: solution structure, entities, pricing calculator with tests, demo catalog page, generated database diagram, documentation.

## Known issues
None open.

## Client questions

Answers received are recorded in [specs](specs/README.md) with the client's original text.

### Answered (2026-11-26) → [answers](specs/2026-11-26-client-answers.md)
- **Taxes:** prices already include 16% IVA; the system only breaks it down ([ADR 0010](decisions/0010-prices-include-iva.md)).
- **Missing price:** a product without a price in the branch's list is not sold. Only Marketing creates items and sets prices ([ADR 0011](decisions/0011-only-marketing-controls-catalog-and-prices.md)).
- **Cash withdrawals:** shown in the shift close, which states how much cash to deposit the next day.
- **Garment status history:** records ticket, guest and garment label per change. Detailed explanation pending.

### Still open
Blocking the **ticket wizard**:
1. **Modifier order:** does Express (+50%) apply on top of fixed extras such as starch?
2. **Manual discounts:** allowed with a supervisor PIN, or not at all?
3. **IVA details:** confirm `IVA = Total − round(Total / 1.16, 2)`, per ticket; any 8% border-zone branch; are CFDI invoices needed?

Blocking the **shift close** and **garment lifecycle**:
4. **Deposit formula:** cash collected − withdrawals? Does the opening float stay in the drawer?
5. **Withdrawal fields:** reason, who authorizes, signature or receipt?
6. **Status history details:** who / when / from → to, location, per garment or per ticket, when to notify the customer.

Not blocking yet:
7. **Several PCs per branch:** affects folio generation, especially offline in Phase 2.
8. **Automatic logout** after how many idle minutes? Manager unlock? PIN length? (see [PIN login](features/pin-login.md))
9. **ID types:** ER rev. 2 mixes integer and UUID keys; we use UUIDs everywhere. Acceptable?
10. **Offline points and wallet (Phase 2):** can points be spent offline? How do we prevent double spending?
11. **Hash chain scope** for the audit log and ledger: per branch, per guest, or global?
12. **Phase 1 scope:** is splitting it into 1a / 1b / 1c acceptable?
