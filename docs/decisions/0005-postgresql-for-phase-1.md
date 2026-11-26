# 0005 · PostgreSQL as the Phase 1 database

- **Status:** Accepted (implemented 2026-11-26)
- **Date:** 2026-11-26

## Context
ER revision 2 defines Phase 1 as a web app on **one central database** shared by all branches. SQLite is a single file with one writer at a time and almost no real types (money and dates are stored as text). The web admin runs on GCP. PostgreSQL 14 is already installed in the development environment.

## Decision
Use PostgreSQL for the central database from Phase 1. Keep SQLite for the offline branch database in Phase 2, with the same EF Core entities.

## Alternatives considered
- **Stay on SQLite:** fine for one user. It fails with "database is locked" under concurrent writes, and it cannot sort by decimal.
- **SQL Server:** the client brief mentions it for analytics, but it has license costs and offers no advantage for this workload. Revisit if the client requires it.
- **MySQL:** viable, but PostgreSQL has stronger types (`uuid`, `numeric`, `timestamptz`, `jsonb`) and is available as GCP Cloud SQL.

## Consequences
- Safe concurrent writes, real types, row locking for folio sequences, managed backups in Cloud SQL.
- Developers need a local PostgreSQL. The migrations are regenerated for the new provider.
- Provider differences show up early, while there is little code.
