# Architecture Decision Records (ADRs)

Each file records **one significant decision**: the context, what was decided, the alternatives, and the consequences. Code shows *what* was done; these explain *why*.

## When to write one

When a decision is hard to reverse, affects several parts of the system, or someone will later ask "why is it like this?".

## How

Copy [template.md](template.md), take the next number, and keep it under a page. Never rewrite an accepted ADR. If a decision changes, write a new ADR that **supersedes** the old one, and update the old one's status.

Statuses: **Proposed** → **Accepted** → (optionally) **Superseded by NNNN**.

## Index

| # | Decision | Status |
|---|---|---|
| [0001](0001-uuid-primary-keys.md) | UUIDs as primary keys | Accepted |
| [0002](0002-code-english-ui-spanish.md) | Code in English, UI in Spanish | Accepted |
| [0003](0003-blazor-for-ui.md) | Blazor (C#) for the UI instead of React | Accepted |
| [0004](0004-shared-ui-multiple-hosts.md) | Shared UI library with multiple hosts | Accepted |
| [0005](0005-postgresql-for-phase-1.md) | PostgreSQL as the Phase 1 database | Accepted |
| [0006](0006-specs-as-guide.md) | Client specs are a guide; deviations are recorded | Accepted |
| [0007](0007-store-facts-derive-totals.md) | Store facts, derive totals | Accepted |
| [0008](0008-snake-case-database-names.md) | snake_case names in the database | Accepted |
| [0009](0009-dev-credentials-in-user-secrets.md) | Development credentials in user secrets | Accepted |
| [0010](0010-prices-include-iva.md) | Prices include IVA; the system only breaks it down | Accepted |
| [0011](0011-only-marketing-controls-catalog-and-prices.md) | Only Marketing controls the catalog and prices | Accepted |
| [0012](0012-percentage-modifiers-on-list-price.md) | Percentage modifiers apply to the list price | Accepted |
| [0013](0013-no-manual-discounts.md) | No manual discounts | Accepted |
