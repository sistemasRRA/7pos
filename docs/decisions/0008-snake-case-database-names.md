# 0008 · snake_case names in the database

- **Status:** Accepted
- **Date:** 2026-11-26

## Context
By default EF Core names tables and columns exactly like the C# classes and properties, in PascalCase (`Tickets.BranchId`). PostgreSQL folds unquoted names to lowercase, so PascalCase names must always be quoted in SQL (`SELECT "Total" FROM "Tickets"`). Forgetting a quote causes an error. The PostgreSQL convention, and what reporting and BI tools expect, is snake_case.

## Decision
Use `EFCore.NamingConventions` (`UseSnakeCaseNamingConvention()`) so the database uses snake_case (`tickets.branch_id`) while C# keeps PascalCase (`ticket.BranchId`). The setting lives in `PosDatabase.Configure`, the single place every entry point (web app, tests, `dotnet ef`) uses to configure the database.

## Alternatives considered
- **Keep PascalCase:** no extra package, but every hand-written query needs quotes.
- **Name each table and column by hand** (`ToTable`, `HasColumnName`): verbose and easy to forget on new properties.

## Consequences
- Hand-written SQL, DBeaver and reports read naturally: `SELECT folio, total FROM tickets`.
- The names in the database differ from the C# names in a predictable way: `BalanceDue` ↔ `balance_due`.
- Adopted before any real data existed, so no data migration was needed.
- The EF migrations history table keeps its default name, `"__EFMigrationsHistory"`.
