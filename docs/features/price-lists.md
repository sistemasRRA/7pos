# Price lists

## What it does
Marketing sets the price of every product per **price list** ("Lista Norte", "Lista Sur"). Each branch sells from one list, so branches in different zones can charge different prices. Branch staff can never create items or type a price ([ADR 0011](../decisions/0011-only-marketing-controls-catalog-and-prices.md)). All prices include IVA ([ADR 0010](../decisions/0010-prices-include-iva.md)).

## Rules

**What a branch sells** (`PriceListService.GetBranchCatalogAsync`)
1. Only **active products that have a price in the branch's list**. Anything else is not offered.
2. The price is the **latest one already in effect**. A price scheduled for a future date applies only once that moment arrives.
3. Each branch sees the prices of **its own** list.
4. A branch without a list sells nothing, and the catalog asks staff to contact Marketing.

**Changing prices** (`PriceListService.SetPriceAsync`)
5. Only an **active user with the Marketing role** may change a price. The role is read from the database on every change, never trusted from the screen.
6. A change **adds a new row** and never edits one ([ADR 0007](../decisions/0007-store-facts-derive-totals.md)). The full history stays, newest first, with who set each price.
7. A price must be greater than 0, in whole cents.
8. A price can start **now or on a future date**, never in the past. On the screen, a future day starts at 00:00 Mexico City time.
9. Every change is written to the audit log as `PRICE_CHANGED`, with the old and new price (list, product, price, start date) as JSON.

**IVA breakdown** (`TaxCalculator.Breakdown`, ADR 0010)
10. `Subtotal = round(Total / 1.16, 2)`, `IVA = Total − Subtotal`. Subtotal + IVA always equals the total; a test checks every amount from $0.01 to $2,000.00.

## Edge cases
- `Product.BasePrice` is now only a **reference** (ER rev. 2). The POS always sells at the list price.
- Modifiers (Almidón, Express…) still have one global price. Prices per list for modifiers can come later if Marketing needs them.
- Two price changes for the same product at the exact same instant would violate the unique index. This is practically impossible from the screen.
- Sample data: development databases load Marketing's 2026 lists (A, AA, B, C) and put every branch on Lista A, plus a Marketing user "Mercadotecnia" with PIN 5678 ([real catalog](real-catalog.md)). Sample data is **never** loaded in production.

## Code
- Rules: [`PriceListService`](../../src/SevenPos.Core/Catalog/PriceListService.cs), [`TaxCalculator`](../../src/SevenPos.Core/Catalog/TaxCalculator.cs)
- Model: [`PriceList`](../../src/SevenPos.Core/Catalog/PriceList.cs), [`PriceListItem`](../../src/SevenPos.Core/Catalog/PriceListItem.cs), `Branch.PriceListId`, `UserRole.Marketing`
- Tests: [`PriceListServiceTests`](../../tests/SevenPos.Core.Tests/PriceListServiceTests.cs), [`TaxCalculatorTests`](../../tests/SevenPos.Core.Tests/TaxCalculatorTests.cs)
- Screens: [`BranchCatalog.razor`](../../src/SevenPos.UI/Catalog/BranchCatalog.razor) (branch prices with IVA), [`Prices.razor`](../../src/SevenPos.UI/Catalog/Prices.razor) (Marketing)

## Not built yet
- Creating or renaming price lists. Assigning a branch its list is built: Precios → Sucursales ([real catalog](real-catalog.md)).
- Creating and deactivating products (Marketing only, per ADR 0011).

## Open questions
- IVA details still to confirm: the rounding method above, per-ticket breakdown, any 8% border-zone branch, CFDI invoicing.
- Should modifiers have prices per list?
