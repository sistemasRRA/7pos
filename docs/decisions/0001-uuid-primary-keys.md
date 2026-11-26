# 0001 · UUIDs as primary keys

- **Status:** Accepted
- **Date:** 2026-11-12

## Context
In Phase 2, branches work offline and sync later. Several branches will create tickets, guests and payments at the same time without talking to each other. The client spec suggested an auto-increment local ID plus a global UUID, and used the guest's phone number as the guest's primary key.

## Decision
Every table uses a UUID (`Id`, from `SyncEntity`) as its only primary key. Business identifiers are separate columns with a unique index: ticket `Folio` (unique per branch), `PublicOrderId`, garment `Code`, guest `Phone`.

## Alternatives considered
- **Auto-increment integers:** two offline branches would both create "ticket 1", and merging them would require rewriting IDs.
- **Local int + global UUID (spec):** two keys to keep in sync, and the foreign key would still need to pick one.
- **Phone as guest key:** phones change, and they are personal data. Changing a primary key cascades through every related table.

## Consequences
- Records can be created anywhere and merged without collisions. Phase 2 needs no data migration.
- IDs are long and not human-friendly. Show folios and codes to people, never UUIDs.
- Indexes are slightly larger than with integers, which is irrelevant at this scale.
