# 0014 · Service lines and modifier groups

- **Status:** Accepted
- **Date:** 2026-11-26

## Context
The first catalog model had a flat list of extras, each asked as a yes/no question. Marketing's 2026 price lists ([third round](../specs/2026-11-26-modifier-groups-and-full-list-a.md)) work differently:
- The catalog is split into **service lines** (Tintorería, Planchado, Sastrería, 7shoes, 7PLUS, Lavandería), and the same garment has a different price in each line.
- Extras come in numbered **groups** (1 Tamaño, 2 Textil…) with several options. The cashier fills them in step order and picks **one option per group**.
- A group applies **only to the products it is assigned to**, and inside a group some options are restricted (Matrimonial and Queen / King only for bed items).

## Decision
- `ServiceLine` is an entity; each `Product` belongs to one (unique name per line).
- `ModifierGroup` belongs to a line; its `SortOrder` is the step number.
- `Modifier` becomes an **option** of a group. Its `SortOrder` only orders options on screen.
- `ProductModifier` keeps assigning **options** (not whole groups) to products. A group is asked when the product has at least one of its options. This one table covers "groups only where assigned" and "bed sizes only for bed items".
- At most one option per group per garment, enforced by `TicketService`.
- The client's CSV transcriptions are the import source (`CatalogImporter`), embedded in `SevenPos.Core`.

## Alternatives considered
- **Assign whole groups to products** (`ProductModifierGroup`): simpler to edit, but cannot express "Tamaño without the bed sizes" without a second exception table.
- **Encode the line in the category** ("Planchado · Ropa de vestir"): duplicates categories and cannot give groups per line.
- **Keep yes/no extras:** cannot express "one of three starch levels" and would let the cashier pick Ligero and Rígido together.

## Consequences
- The wizard asks fewer, clearer questions, in the order Marketing defined.
- Editing assignments means editing option rows. A future Marketing screen should offer "whole group" shortcuts.
- Until Marketing gives the real assignments, the importer assigns every option of a line to each of its products, except the bed sizes.
