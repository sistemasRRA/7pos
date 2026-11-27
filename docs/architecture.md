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

1. `Catalog/BranchCatalog.razor` asks `PriceListService.GetBranchCatalogAsync` for what the branch sells, with its list prices.
2. The cashier picks a product (`Tickets/ProductPicker.razor`) and answers its modifier groups step by step (`Tickets/GarmentCapture.razor`).
3. The component calls `PriceCalculator.Calculate(listPrice, modifiers)` in Core.
4. The calculator applies the modifiers in step order, every percentage on the list price, and returns the total and each charge.
5. The component displays the result. When the ticket is saved, `TicketService` prices it again from the database; the screen's numbers are only a preview.

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
├── Catalog/       ServiceLine, Category, Product, ModifierGroup, Modifier, ProductModifier,
│                  PriceList, PriceListItem, PriceCalculator, TaxCalculator,
│                  PriceListService, CatalogImporter
├── Guests/        Guest, GuestAddress, PhoneNumber, GuestService
├── Tickets/       Ticket, Garment, GarmentModifier, TicketService
├── Cash/          Shift, Payment, CashWithdrawal, ShiftService, PaymentService
├── Data/          PosDbContext, PosDatabase, SeedData, Migrations
└── GlobalUsings.cs  imports every area's namespace once for the whole project
```

The namespace matches the folder (`SevenPos.Core.Tickets`). When you work on shift closing, everything you need is in `Cash/`. This "modular monolith" keeps each area easy to find and change, and makes it easy to split out later if that is ever needed.

A new business area gets its own folder plus a line in `GlobalUsings.cs`.

## Folder conventions inside UI

`SevenPos.UI` uses **the same areas as Core**. Each folder holds that area's pages and the components they use, side by side:

```
SevenPos.UI/
├── Tickets/   TicketWizard (page /tickets/new), TicketDetails, GarmentCapture, ProductPicker, LineList, Totals, TicketLine
├── Guests/    GuestSearch (page /guests), GuestPicker, GuestForm, GuestCard
├── Catalog/   BranchCatalog (page /), Prices (page /prices)
├── Cash/      CashDrawer (page /cash), CollectPayment (page /pay), PaymentForm, PaymentList, Withdrawals
├── Login/     PinLogin (page /login), PinPad
├── Layout/    PosLayout, BlankLayout
├── Display.cs how values are shown (money, dates, extras)
└── _Imports.razor  one @using per area
```

A component used by several areas stays in the area it belongs to (`GuestPicker` lives in `Guests/` and the ticket wizard uses it). Each component's scoped CSS (`X.razor.css`) sits next to it; VS Code nests it under the `.razor` file (`.vscode/settings.json`).
