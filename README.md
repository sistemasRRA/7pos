# 7Pos

Point of sale and back office for a chain of dry cleaners and laundries in Mexico (own branches and franchises). It covers garment intake, a step-by-step pricing wizard, deposits and settlements, shift closing (Corte X/Z), and tracking each garment until delivery.

> **Convention:** all code is in **English**; everything the end user sees is in **Spanish**.

## Quick start

Requirements: .NET 8 SDK, git and PostgreSQL 14+ (with the `sevenpos` user, see getting started). See [docs/getting-started.md](docs/getting-started.md) for full setup.

```bash
dotnet tool restore                      # install project tools (once)
dotnet test                              # run the tests
dotnet run --project src/SevenPos.Web    # open http://localhost:5175
```

Log in with the sample user **Admin**, PIN **1234**.

## Documentation

| Read this | When |
|---|---|
| [Getting started](docs/getting-started.md) | Setting up your machine for the first time |
| [Architecture](docs/architecture.md) | Understanding how the projects and layers fit together |
| [Data rules](docs/data-rules.md) | Before touching entities or the database |
| [Database diagram](docs/database.md) | Seeing the current tables and relationships |
| [Glossary](docs/glossary.md) | Translating client terms (Spanish) to code names |
| [Decisions](docs/decisions/README.md) | Understanding *why* things are the way they are |
| [Features](docs/features/README.md) | Business rules of each module |
| [Roadmap](docs/roadmap.md) | Phases, what is next, open questions for the client |
| [Client specs](docs/specs/README.md) | The original requirement documents |
| [Contributing](CONTRIBUTING.md) | Conventions and day-to-day workflow |
