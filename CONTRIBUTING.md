# Contributing

## Language

- **Code is English:** files, folders, classes, variables, database tables and columns, comments, test names, commit messages and docs.
- **The UI is Spanish:** labels, messages, printed tickets, and catalog data such as "CAMISA" or "Almidón: Rígido".
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

### Razor conventions

- Name injected services after their type: `@inject GuestService GuestService`. A shorter name such as `Guests` can clash with the page's own class name (`Guests.razor` → error CS0542).
- Do not name a page after a type it uses. `NewTicket.razor` would compile to a class `NewTicket` and hide the `NewTicket` record from Core; the page is `TicketWizard.razor`.
- Pages own their state and layout. Pieces a future screen will reuse (a form, a card, a keypad) go in `Components/` and talk to the page through `[Parameter]` values and `EventCallback`s.
- Spanish text lives in the `.razor` file. The service returns codes (`GuestError.InvalidPhone`) and the component turns them into messages.

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

## Branches

```
main      released code; only receives merges from dev
└── dev   integration branch; always builds and passes its tests
    └── feature/…, fix/…, refactor/…   your work, created from the latest dev
```

1. Update dev first (`git switch dev && git pull`), then branch from it: `feature/pin-login`, `fix/shift-rounding`, `refactor/migrate-to-postgresql`. Use lowercase kebab-case.
2. Open a **pull request into `dev`** when the work is ready. Use the template; it asks for what changed, why, and how it was tested.
3. Merge only when the tests pass and the manual checks in the PR are done.
4. Delete the branch after merging.

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org): `type(scope): summary`.

| Type | For |
|---|---|
| `feat` | New behavior for users |
| `fix` | Bug fix |
| `refactor` | Code change with no behavior change |
| `test` | Adding or fixing tests only |
| `docs` | Documentation only |
| `chore` | Tooling, dependencies, config |

The scope is the business area or project: `organization`, `catalog`, `tickets`, `cash`, `ui`, `db`.

- Summary: imperative, lowercase, no period, max ~70 characters: `feat(organization): add PIN lockout fields to User`
- Body: **what** changed and **why**, as bullets. Mention migrations and anything a reviewer must know.
- **One commit per logical section** (data model, rules + tests, UI, docs). Each commit must build and pass its tests on its own.

## Documentation

Docs live in this repo and change **in the same commit** as the code they describe. Keep them short. Generate what can be generated (for example the database diagram). Do not draw by hand what the code already knows.
