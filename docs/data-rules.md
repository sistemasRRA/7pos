# Data rules

Rules for anyone adding or changing entities. They protect money and history, and most were learned the hard way in other POS systems.

## 1. Money is `decimal`, never `float` or `double`

`0.1 + 0.2` in floating point is `0.30000000000000004`. In a cash register that becomes a shortage. All money columns are `decimal(12,2)` (configured globally in `PosDbContext`). Round with `PriceCalculator.Round` (2 decimals, half away from zero).

## 2. Nothing is deleted

- Catalog records are **deactivated**: `IsActive = false`.
- Tickets are **cancelled**: `Status = Cancelled` plus a `CancellationReason`. The cancellation is written to `AuditLog`.
- Never call `Remove()` on business data.

## 3. Freeze prices at the moment of sale

If marketing raises a shirt from $50 to $60 tomorrow, yesterday's ticket must still say $50. That is why `Garment.PriceCharged` and `GarmentModifier.ChargeApplied` copy the price when the garment is sold. Never recalculate old tickets from the current catalog.

## 4. Store facts, derive totals

Record each event (a payment, a points movement, a status change) as its own row. Calculate totals from those rows. For example, shift totals per payment method are summed from `Payments`; they are not kept in a separate counter that can drift. See [ADR 0007](decisions/0007-store-facts-derive-totals.md).

## 5. Every table has a UUID primary key

Business numbers (ticket folio, garment label code, guest phone) are separate columns with a `UNIQUE` index, never the primary key. See [ADR 0001](decisions/0001-uuid-primary-keys.md).

## 6. Let the database protect you

If something must be true, make the database enforce it:

- Unique values → `HasIndex(...).IsUnique()`
- Required values → non-nullable property or `required`
- References to another table → a **real foreign key**

EF Core only creates a foreign key when the entity has a navigation property (`public Branch? Branch { get; set; }`) or the relationship is configured in `OnModelCreating`. A bare `Guid BranchId` is **not** a foreign key. Check [database.md](database.md) after each migration: a reference without `FK` is a bug.

## 7. Dates are UTC

Store `DateTime` in UTC (properties end in `Utc`). Convert to Mexico's time zone only for display.

## 8. Enums are stored as text

The database stores `"Cash"`, not `0`, so it stays readable and reordering an enum cannot corrupt data. Enum values are English. The UI shows them in Spanish through display labels, never the raw name.

## 9. Sync fields

Every entity inherits `SyncEntity` (`Id`, `LastModifiedUtc`, `SyncStatus`, `IsActive`). `PosDbContext` stamps `LastModifiedUtc` and sets `SyncStatus = Pending` on every save. Do not set them by hand. They become essential in Phase 2 (offline branches).

## 10. Changing the schema

Change the C# entity, then create a migration. Never alter the database by hand. Read the generated migration before committing. See [CONTRIBUTING](../CONTRIBUTING.md#database-changes).
