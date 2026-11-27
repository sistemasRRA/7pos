# Features

One file per business area, written **when the feature is built** (not before). Each file explains the business rules in plain language, links to the code and tests, and lists the edge cases.

| Feature | Status |
|---|---|
| [Pricing](pricing.md) | Built (base price + modifiers) |
| [PIN login](pin-login.md) | Built |
| [Guests](guests.md) (search and register) | Built |
| [Price lists](price-lists.md) (per zone, IVA breakdown) | Built |
| [Ticket capture wizard](ticket-wizard.md) | Built (no discounts or payment yet) |
| [Real catalog](real-catalog.md) (service lines, groups step by step, 2026 lists, branch areas) | Built for Tintorería, Planchado, 7PLUS |
| [Shifts](shifts.md) (open, withdrawals, Corte Z with blind count) | Built (Corte X and deposit wait for the client) |
| Payments (advance and settlement) | Planned |
| Garment lifecycle | Planned |

Use this outline for new feature docs:

```markdown
# Feature name
## What it does        (2–3 sentences, business language)
## Rules               (numbered; each one should have a test)
## Edge cases          (what happens when…)
## Code                (links to services, pages, tests)
## Open questions
```
