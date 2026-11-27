# Client specifications

The original requirement documents from the client. They are **inputs, not the source of truth for the code**: see [ADR 0006](../decisions/0006-specs-as-guide.md) for how they are used, and the [glossary](../glossary.md) for how their terms map to code names.

| File | Content |
|---|---|
| `BD y ER 7Pos 2.0.pdf` | First database schema and ER diagram (all modules) |
| `Brief tecnico 7Pos 2.0.pdf` | Technical brief: modules, stack, features of every app |
| `7Pos V4 · Modelo ER.html` | ER revision 2: phases, promotions/coupons/loyalty engine (open in a browser) |
| [2026-11-26-client-answers.md](2026-11-26-client-answers.md) | Client answers on IVA, prices, cash withdrawals and status history, with our interpretation |
| [2026-11-26-modifiers-discounts-price-lists.md](2026-11-26-modifiers-discounts-price-lists.md) | Percentages on the list price, no manual discounts, and Marketing's 2026 price lists |
| `MKT Lista Precios 2026 A / AA / B / C Simplificada.pdf` | Marketing's price lists (the full A has every service line); transcribed to [price-lists-2026.csv](2026-11-26-price-lists-2026.csv) and [modifiers-2026.csv](2026-11-26-modifiers-2026.csv) |
| [2026-11-26-modifier-groups-and-full-list-a.md](2026-11-26-modifier-groups-and-full-list-a.md) | Modifier groups (order, one option per group, assigned per product) and the full list A |

When the client sends a new version, add it here with a date or revision number. Do not overwrite the old one, because decisions refer to specific versions.
