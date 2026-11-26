# 0002 · Code in English, UI in Spanish

- **Status:** Accepted
- **Date:** 2026-11-12

## Context
The client and the end users are Mexican, and the specs are in Spanish. C#, .NET, EF Core and almost all documentation and libraries are in English. The first version of the code used Spanish names, which produced mixed lines such as `db.Invitados.Where(i => i.IsActive)`.

## Decision
- Everything in the code is English: identifiers, files, tables, columns, comments, tests, commits and docs.
- Everything the user sees is Spanish: labels, messages, tickets and catalog data.
- A [glossary](../glossary.md) maps client terms to code names.

## Alternatives considered
- **All Spanish:** mixes with English framework APIs, accents are awkward in identifiers, and it is harder for non-Spanish-speaking developers.
- **Mixed:** the worst of both.

## Consequences
- Code reads consistently and matches online examples.
- Developers must translate client terms. The glossary must be kept up to date.
- Enum values are English in the database, so the UI needs Spanish display labels for them.
