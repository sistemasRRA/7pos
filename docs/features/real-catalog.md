# Real catalog (2026 price lists)

## What it does
Loads Marketing's 2026 price lists into the POS and sells them the way the client works: products belong to a **service line** (Tintorería, Planchado, 7PLUS…), and the extras are **groups** asked **step by step** in the order of the lists (1 Tamaño, 2 Textil, 3 Suciedad, 4 Relleno, 5 Espesor, 6 Almidón, 7 Extra, 8 Urgente), one option or none per group. Each branch sells from the list (A, AA, B, C) of the **area where it is located**, and Marketing sets that list per branch.

Source: [client answers, third round](../specs/2026-11-26-modifier-groups-and-full-list-a.md) and the transcribed CSVs in [docs/specs](../specs/README.md).

## Rules

**Model** ([ADR 0014](../decisions/0014-service-lines-and-modifier-groups.md))
1. Each product belongs to one **service line**. The same garment can be in several lines at different prices (CAMISA: Tintorería $89, Planchado $49).
2. A **modifier group** belongs to a line and has a step number (`SortOrder`). Its options are `Modifier` rows.
3. A product offers **only the options assigned to it** (`ProductModifier`). A group with no assigned option is never asked for that product.

**Capture** (`TicketService`, `GarmentCapture`)
4. The wizard asks the groups in step order. For each one the cashier picks **one option or "Ninguno"**; "Atrás" changes the last answer.
5. A garment with two options of the same group is refused (`MoreThanOneOptionPerGroup`).
6. Every percentage is of the list price ([ADR 0012](../decisions/0012-percentage-modifiers-on-list-price.md)): an EDREDON at $235, Mediano (15%) and Urgente (50%) costs $235 + $35.25 + $117.50 = $387.75.

**Import** (`CatalogImporter`)
7. Only an active Marketing user can import. Each import is audited as `CATALOG_IMPORTED` with what was added and skipped.
8. Lines, groups, options, categories and products are matched by name and created only if missing. A price row is added only if the list has no price for that product or a different one, so importing twice adds nothing.
9. Every product gets all the options of its line, **except the bed sizes (Matrimonial, Queen / King), which only "Ropa de cama y blancos" gets** (client rule).
10. Rows with a $0 price and repeated products are skipped and reported.
11. `Import2026Async` loads the lines whose data is ready: **Tintorería, Planchado and 7PLUS** (136 products, 16 groups, 41 options, 307 prices). It skips 3 rows: the two "Política de calidad" items ($0) and the second Planchado SUDADERA ($79).

**Areas** (`PriceListService.AssignBranchListAsync`)
12. Only Marketing sets a branch's list, from **Precios → Sucursales**. Changes are audited as `BRANCH_PRICE_LIST_CHANGED` with the old and new list.

## Edge cases
- **Branches on lists AA, B or C** sell only Tintorería for now: the 2026 PDFs give Planchado and 7PLUS prices only for list A, and a product without a price in the branch's list is not sold ([ADR 0011](../decisions/0011-only-marketing-controls-catalog-and-prices.md)).
- **Sample data:** development databases load the 2026 catalog and put every branch on Lista A. The first sample catalog ("Camisa de vestir"…, line "General", "Lista General") is deactivated, never deleted.
- **Older databases:** the migration moves existing products to a "General" line and turns each old extra into a group with one option.
- Tickets already created keep their frozen prices and extras.

## Code
- Model: [`ServiceLine`](../../src/SevenPos.Core/Catalog/ServiceLine.cs), [`ModifierGroup`](../../src/SevenPos.Core/Catalog/ModifierGroup.cs), [`Modifier`](../../src/SevenPos.Core/Catalog/Modifier.cs), [`ProductModifier`](../../src/SevenPos.Core/Catalog/ProductModifier.cs)
- Rules: [`CatalogImporter`](../../src/SevenPos.Core/Catalog/CatalogImporter.cs), [`TicketService`](../../src/SevenPos.Core/Tickets/TicketService.cs), [`PriceListService`](../../src/SevenPos.Core/Catalog/PriceListService.cs)
- Tests: [`CatalogImporterTests`](../../tests/SevenPos.Core.Tests/CatalogImporterTests.cs), [`TicketServiceTests`](../../tests/SevenPos.Core.Tests/TicketServiceTests.cs), [`PriceListServiceTests`](../../tests/SevenPos.Core.Tests/PriceListServiceTests.cs), [`DatabaseTests`](../../tests/SevenPos.Core.Tests/DatabaseTests.cs)
- Screens: [`ProductPicker`](../../src/SevenPos.UI/Tickets/ProductPicker.razor), [`GarmentCapture`](../../src/SevenPos.UI/Tickets/GarmentCapture.razor), [`TicketWizard.razor`](../../src/SevenPos.UI/Tickets/TicketWizard.razor), [`BranchCatalog.razor`](../../src/SevenPos.UI/Catalog/BranchCatalog.razor), [`Prices.razor`](../../src/SevenPos.UI/Catalog/Prices.razor)

## Not built yet
- **Sastrería, 7shoes and Lavandería** (waiting for the client, see below).
- A screen for Marketing to change which options a product offers, or to create products and groups. Today the importer's rule decides.
- Importing a new price list file from the app (today the 2026 lists load as sample data in Development only).
- Real branches in production: they need their list assigned (Precios → Sucursales) and their code set.

## Open questions (on hold)
See the [third round](../specs/2026-11-26-modifier-groups-and-full-list-a.md#questions-found-in-the-full-list-on-hold): which groups each product really gets (today: all of its line), Sastrería "jr 205%", the duplicated SUDADERA, 7shoes charges, Lavandería by the kilo, prices of Planchado and 7PLUS in lists AA, B and C, and which branch uses which list.
