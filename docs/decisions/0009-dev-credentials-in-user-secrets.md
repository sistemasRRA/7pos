# 0009 · Development credentials in user secrets

- **Status:** Accepted
- **Date:** 2026-11-26

## Context
The local PostgreSQL password was committed in three places: `appsettings.Development.json`, `PosDatabase.LocalDevConnection` (used by tests and `dotnet ef`) and `tools/db_diagram.py`. Even a development-only password in Git is a bad habit. It gets copied to real environments, it spreads to every clone, and automated tooling flagged it when pushing.

## Decision
- No connection string with credentials is stored in the repository.
- Locally, each developer stores it once with `dotnet user-secrets` on `SevenPos.Web`. The test project shares the same `UserSecretsId`, and `tools/db_diagram.py` reads the same secrets file, so there is one place to configure.
- Other environments use environment variables: `ConnectionStrings__Pos`, plus `SEVENPOS_TEST_DB` for tests.
- Generating migrations does not connect to a database, so `DesignTimeFactory` uses a connection string without credentials.

> **Update 2026-11-26:** some `dotnet ef` commands do connect (`migrations remove`, `migrations list`, `database update`), and `migrations remove` failed without credentials. `DesignTimeFactory` now also reads the shared user secret (SevenPos.Core has the same `UserSecretsId`). It falls back to a connection string without credentials only when no secret is configured.

## Alternatives considered
- **Keep the password in appsettings.Development.json:** simple, but it is still a secret in Git.
- **Environment variables only:** works, but they are easy to lose between terminals and VS Code sessions, and are not the .NET convention for local development.

## Consequences
- New developers run one extra command (in [getting started](../getting-started.md)), and the app and tests explain the command if it is missing.
- The old development password remains in the Git history of earlier commits. It only ever worked on local machines. Developers who used it should change it (`ALTER ROLE sevenpos PASSWORD '…'`) and update their user secret.
