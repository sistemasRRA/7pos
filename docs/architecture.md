# Architecture

## The big picture

7Pos is one product line with several apps, built in **phases** (see [roadmap](roadmap.md)):

| App | Who uses it | Phase |
|---|---|---|
| **7Pos** (branch POS) | Cashiers, back-of-house staff | 1 (web), 2 (Windows desktop, offline) |
| **Web Admin** | Directors, marketing, franchise owners | 1+ |
| **CleanCom / 7clean** | Drivers (mobile), dispatcher | 2 |
| **Guest portal** | Customers (PWA) | 3 |
| **7Wash / 7clean dark** | Self-service kiosks, smart lockers | 4 |

This repository currently contains the **7Pos branch POS**, Phase 1.

## Layers

```
┌─────────────────────────────┐
│  UI  (SevenPos.UI)          │  Blazor screens. Shows data, reacts to clicks.
│                             │  Contains NO business rules.
└──────────────┬──────────────┘
               │ calls
┌──────────────▼──────────────┐
│  Business logic             │  Prices, validations, totals, shift close.
│  (SevenPos.Core/<area>)     │  Written once, tested, reused by every app.
└──────────────┬──────────────┘
               │ reads / writes
┌──────────────▼──────────────┐
│  Data (SevenPos.Core/Data)  │  EF Core DbContext, entities, migrations.
│  + database                 │  The database enforces integrity (keys, uniques).
└─────────────────────────────┘
```

**The one rule:** business logic never lives in a `.razor` file. If it did, the drivers' app, the kiosk and the Web Admin would each need a copy.

## Projects

```
SevenPos.sln
├── src/SevenPos.Core         class library — entities, DbContext, migrations, services. No UI.
├── src/SevenPos.UI           Razor class library — pages and components (shared by every host)
├── src/SevenPos.Web          ASP.NET Core host — runs the UI in the browser (Blazor Server)
└── tests/SevenPos.Core.Tests xUnit tests for Core
```

Dependencies only point **downwards**: `Web → UI → Core`. Core knows nothing about screens.

### Hosts: one UI, several shells

The screens live in `SevenPos.UI` so that different "shells" can run them unchanged:

```
SevenPos.UI + SevenPos.Core
        ├── SevenPos.Web  → browser (today, Phase 1)
        └── SevenPos.App  → Windows desktop via .NET MAUI Blazor Hybrid (Phase 2)
```

The desktop shell is needed for what a browser cannot do: work fully offline, read the hardware ID for licensing, and send RAW commands to thermal and label printers. See [ADR 0004](decisions/0004-shared-ui-multiple-hosts.md).

## How a request flows (example: pricing a garment)

1. `Pages/Catalog.razor` loads categories, products and modifiers through `IDbContextFactory<PosDbContext>`.
2. The cashier picks a product and its modifiers.
3. The page calls `PriceCalculator.Calculate(basePrice, modifiers)` in Core.
4. The calculator applies the modifiers in `SortOrder` and returns the total and each charge.
5. The page displays the result.

## Technology

| Concern | Choice | Why |
|---|---|---|
| Language | C# / .NET 8 | Required by the client brief; shared with the mobile app ([ADR 0003](decisions/0003-blazor-for-ui.md)) |
| UI | Blazor | Same language as the logic; runs in browser and desktop |
| Data access | EF Core 8 | Tables from C# classes, migrations, LINQ queries |
| Database | PostgreSQL (central, Phase 1); SQLite returns for the offline branch app in Phase 2 | Concurrency, real types, Cloud SQL on GCP ([ADR 0005](decisions/0005-postgresql-for-phase-1.md)) |
| Tests | xUnit | |

## Folder conventions inside Core

Core is organized by **business area**, not by type of file. Each area holds its entities **and** its services, one type per file:

```
SevenPos.Core/
├── Common/        SyncEntity (base class with the sync fields), BusinessTime (Mexico City time)
├── Organization/  Branch, User, Attendance, AuditLog, PinHasher
├── Catalog/       Category, Product, Modifier, ProductModifier, PriceList, PriceListItem,
│                  PriceCalculator, TaxCalculator, PriceListService
├── Guests/        Guest, GuestAddress, PhoneNumber, GuestService
├── Tickets/       Ticket, Garment, GarmentModifier, TicketService
├── Cash/          Shift, Payment, CashWithdrawal
├── Data/          PosDbContext, PosDatabase, SeedData, Migrations
└── GlobalUsings.cs  imports every area's namespace once for the whole project
```

The namespace matches the folder (`SevenPos.Core.Tickets`). When you work on shift closing, everything you need is in `Cash/`. This "modular monolith" keeps each area easy to find and change, and makes it easy to split out later if that is ever needed.

A new business area gets its own folder plus a line in `GlobalUsings.cs`.
