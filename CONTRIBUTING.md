# Contributing

## Language

- **Code is English:** files, folders, classes, variables, database tables and columns, comments, test names, commit messages and docs.
- **The UI is Spanish:** labels, messages, printed tickets, and catalog data such as "Camisa de vestir".
- New business concept from the client specs? Pick an English name and **add it to the [glossary](docs/glossary.md)** in the same commit.

## Where code goes

| You are writing… | Put it in |
|---|---|
| A table / entity | `src/SevenPos.Core/<Area>/` — e.g. `Tickets/Garment.cs`, one type per file |
| A business rule (prices, validations, totals) | `src/SevenPos.Core/<Area>/` next to its entities, e.g. `Catalog/PriceCalculator.cs`. **Never** inside a `.razor` file |
| A screen or reusable component | `src/SevenPos.UI/Pages` or `src/SevenPos.UI/Components` |
| Startup and configuration | `src/SevenPos.Web/Program.cs` |
| Tests | `tests/SevenPos.Core.Tests` |

See [architecture](docs/architecture.md) for the reasoning.

## Building a feature

Always in this order:

1. **Model the data.** Add or change the entities, then create a migration (see below).
2. **Write the rule.** Put it in a service in `Core`.
3. **Test the rule.** Cover the normal case and the cases that must fail.
4. **Build the screen.** The page calls the service.
5. **Document.** Update the feature doc, the glossary and the diagram. Add an ADR if you made a significant decision.

## Database changes

Never edit the database by hand. Change the entity class, then:

```bash
dotnet ef migrations add DescribeTheChange --project src/SevenPos.Core --output-dir Data/Migrations
dotnet run --project src/SevenPos.Web      # applies the migration on startup
python3 tools/db_diagram.py                # refreshes docs/database.md
```

Review the generated migration file before committing; it is the actual SQL change. Read [data rules](docs/data-rules.md) first.

## Tests

`dotnet test` must pass before every commit. Every business rule gets a test. Name tests as sentences: `Percentage_IsRoundedToCents`.

## Commits and branches

- `main` must always build and pass its tests.
- Work on a branch: `feature/pin-login`, `fix/shift-rounding`.
- Commit messages in the imperative: "Add PIN login", "Fix express surcharge rounding".

## Documentation

Docs live in this repo and change **in the same commit** as the code they describe. Keep them short. Generate what can be generated (for example the database diagram). Do not draw by hand what the code already knows.
