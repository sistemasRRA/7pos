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

Recommended: **DBeaver** (free, install on Windows) to browse data, run SQL and auto-generate ER diagrams. Connect to `localhost:5432`, database `sevenpos`, user `sevenpos`, password `sevenpos_dev` (WSL forwards the port to Windows).

## 3. Database (once per machine)

Install PostgreSQL if needed (`sudo apt install postgresql`), then create the development user and database:

```bash
sudo -u postgres psql -c "CREATE ROLE sevenpos WITH LOGIN PASSWORD 'sevenpos_dev' CREATEDB;"
sudo -u postgres psql -c "CREATE DATABASE sevenpos OWNER sevenpos;"
```

- `CREATEDB` lets the tests create and drop throwaway databases.
- This password is for local development only. It lives in `src/SevenPos.Web/appsettings.Development.json`. Real environments pass the connection string through the `ConnectionStrings__Pos` environment variable.
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
4. Open `src/SevenPos.UI/Pages/Catalog.razor` and `src/SevenPos.Core/Services/PriceCalculator.cs` to see a full slice from screen to rule.

## Later: the Windows desktop app

Phase 2 adds a .NET MAUI Blazor Hybrid app for the branch PCs. It **must be built on Windows** (Visual Studio 2022 or VS Code with C# Dev Kit), not inside WSL. Until then, everything runs in the browser.
