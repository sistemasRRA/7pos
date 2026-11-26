# Getting started

This guide takes a new machine to a running 7Pos. If something here is wrong or missing, fix this file.

## 1. Environment

Development happens on **Windows with WSL 2 (Ubuntu)**. The code lives inside WSL (e.g. `~/7pos`).

| Tool | Why | Check |
|---|---|---|
| git | Source control | `git --version` |
| .NET 8 SDK | Build and run | `dotnet --version` → `8.x` |
| PostgreSQL 14+ | Database | `psql --version` |
| Python 3 | Diagram script | `python3 --version` |
| VS Code + WSL extension | Editor | Open the folder with `code .` from WSL |

Install the .NET 8 SDK following Microsoft's instructions for Ubuntu: https://learn.microsoft.com/dotnet/core/install/linux-ubuntu

## 2. Recommended VS Code extensions

- **C# Dev Kit** (`ms-dotnettools.csdevkit`): IntelliSense, debugging, tests
- **Markdown Preview Mermaid Support** (`bierner.markdown-mermaid`): renders the diagrams in `docs/`

Recommended: **DBeaver** (free, install on Windows) to browse data, run SQL and auto-generate ER diagrams. Connect to `localhost:5432`, database `sevenpos`, user `sevenpos`, with the password you chose in step 3 (WSL forwards the port to Windows).

## 3. Database (once per machine)

Install PostgreSQL if needed (`sudo apt install postgresql`). Pick a password for your local database user, then create the user and the database:

```bash
sudo -u postgres psql -c "CREATE ROLE sevenpos WITH LOGIN PASSWORD '<your password>' CREATEDB;"
sudo -u postgres psql -c "CREATE DATABASE sevenpos OWNER sevenpos;"
```

Then store the connection string **once** with .NET user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:Pos" \
  "Host=localhost;Port=5432;Database=sevenpos;Username=sevenpos;Password=<your password>" \
  --project src/SevenPos.Web
```

- **Never put passwords in the repository.** User secrets live in `~/.microsoft/usersecrets/`, outside the project. The app, the tests and `tools/db_diagram.py` all read this one secret. Check it with `dotnet user-secrets list --project src/SevenPos.Web`.
- `CREATEDB` lets the tests create and drop throwaway databases.
- Other environments (CI, servers) pass the connection string through the `ConnectionStrings__Pos` environment variable. Tests also accept `SEVENPOS_TEST_DB`.
- The app creates the tables itself (migrations run on startup).

## 4. First run

```bash
git clone <repo-url> 7pos && cd 7pos
dotnet tool restore                      # installs dotnet-ef (EF Core CLI)
dotnet test                              # all tests should pass
dotnet run --project src/SevenPos.Web    # http://localhost:5175
```

On first run the app applies all migrations and loads sample data: branch "Sucursal Demo", user Admin with PIN 1234, and a small catalog.

To start over with a clean database:

```bash
sudo -u postgres psql -c "DROP DATABASE sevenpos;" -c "CREATE DATABASE sevenpos OWNER sevenpos;"
```

## 5. Next steps

1. Read [architecture](architecture.md) (10 minutes).
2. Read [data rules](data-rules.md) before touching entities.
3. Skim the [decisions](decisions/README.md) and the [glossary](glossary.md).
4. Log in as Admin / 1234 and try three wrong PINs to see the lockout. Then open **Clientes**, register a guest and search for them by the last 4 digits of their phone.
5. Open `src/SevenPos.UI/Pages/Catalog.razor` and `src/SevenPos.Core/Catalog/PriceCalculator.cs` to see a full slice from screen to rule.

## Later: the Windows desktop app

Phase 2 adds a .NET MAUI Blazor Hybrid app for the branch PCs. It **must be built on Windows** (Visual Studio 2022 or VS Code with C# Dev Kit), not inside WSL. Until then, everything runs in the browser.
