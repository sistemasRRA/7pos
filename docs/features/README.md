# Features

One file per business area, written **when the feature is built** (not before). Each file explains the business rules in plain language, links to the code and tests, and lists the edge cases.

| Feature | Status |
|---|---|
| [Pricing](pricing.md) | Built (base price + modifiers) |
| [PIN login](pin-login.md) | Built |
| [Guests](guests.md) (search and register) | Built |
| [Price lists](price-lists.md) (per zone, IVA breakdown) | Built |
| Ticket capture wizard | Planned |
| Payments and deposits | Planned |
| Shift open / close (Corte X/Z) | Planned |
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
