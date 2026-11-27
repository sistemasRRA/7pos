# Client answers · 2026-11-26 (third round)

Answers to the follow-ups on modifier groups in the [second round](2026-11-26-modifiers-discounts-price-lists.md#3-the-2026-price-lists), and the **full** list A (`MKT Lista Precios 2026 A Simplificada.pdf`, 24 pages), which covers every service line, not only dry cleaning.

## Original text

> del 1 al ocho es como se debe que llenar la informacion. por ejemplo si es edredon y es mediano se le cobra un 15% extra y asi hasta hacer el desgloce
>
> 2 si
>
> 3 no solo a la que se le asigne
>
> los demas puntos los dejemos en stand by

## Interpretation

1. **The step numbers are the wizard's order.** The cashier fills in the groups in that order (Tamaño, Textil, Suciedad…). E.g. an EDREDON that is "Mediano" gets +15% of its list price, then the next group, until the breakdown is complete.
2. **One option per group.** A garment gets at most one option of each group, e.g. "Aplicaciones" *or* "Prenda delicada", not both.
3. **Groups apply only to the products they are assigned to.** Marketing decides which groups each product asks. The PDFs do not say which products get which groups, so the assignment has to come from Marketing (or a default, see below).
4. The other questions (the $0 "Política de calidad" items, which branch uses which list, whether franchises may change the suggested price) are **on hold**.

## Clarification: the list depends on the branch's area
Relayed by the developer: the list (A, AA, B, C) changes the prices **according to the area where the branch is located**. So the system must know the area of each installation.

This is how the model already works: each branch points to one price list (`Branch.PriceListId`, ER rev. 2 `Matriz_Precios_ID`), and the POS sells at the prices of the branch it runs in. A separate "area" table is not needed unless an area has other properties (a name such as "Polanco", its own promotions, an 8% border-zone IVA). Still to build:
- A screen for Marketing to assign each branch its list (today only sample data does it).
- Device registration (`DISPOSITIVOS`, ER rev. 2), so each installation knows its branch. Today the terminal uses the first active branch.

## Corrections to the second round
The first copy of list A was incomplete:
- List A **does** have SLEEPING, COBERTOR, COLCHA, CORTINA, EDREDON and CUBRE COLCHÓN, at **$235** (page 2).
- List A **does** have the Tamaño and Relleno groups (page 3), with the same percentages as AA, B and C.

## What the full list A contains
Transcribed to [price-lists-2026.csv](2026-11-26-price-lists-2026.csv) (194 rows, column `service`) and [modifiers-2026.csv](2026-11-26-modifiers-2026.csv) (62 options).

| Service line | Products | Groups, in wizard order | Notes |
|---|---|---|---|
| **Tintorería** (dry cleaning) | 59, lists A, AA, B, C | 1 Tamaño, 2 Textil, 3 Suciedad, 4 Relleno, 5 Espesor, 6 Almidón, 7 Extra, 8 Urgente | The only line with four lists |
| **Planchado** (ironing) | 63, list A | 1 Tamaño, 2 Textil, 3 Almidón, 4 Urgente | SUDADERA appears twice ($49 and $79) |
| **Sastrería** (tailoring) | 39 processes, list A | 1 Tamaño, 2 Dificultad, 3 Material adicional, 4 Urgente | "COTIZAR SERVICIO" is $0 (quoted per job); "ZURCIDO INVISIBLE POR CM." may be per centimeter |
| **7shoes** (shoe care) | 5 shoe types × Limpieza, Especiales, Restauración | Suciedad, Textil, Extra, Adicional, Urgente | Especiales and Restauración are $0; see below |
| **7PLUS** (leather and premium) | 17, list A | 1 Tamaño, 2 Suciedad, 3 Extra, 4 Urgente | |
| **Lavandería** (laundry) | 1: KILO LAVANDERÍA $59 | none | Sold by the kilo, minimum 3 kilos |

The same garment can appear in several lines at different prices (CAMISA: Tintorería $89, Planchado $49), so a product is **a garment within a service line**.

## What this means for the model
- **Service line** (Tintorería, Planchado…): each product belongs to one.
- **Modifier groups** with a step number and options. The cashier picks **at most one option per group**. Groups belong to a service line.
- Products are assigned **groups**, not individual extras.
- Percentages are of the list price ([ADR 0012](../decisions/0012-percentage-modifiers-on-list-price.md)). One fixed charge exists (7shoes "Ultra white" $159).
- **Default for the assignment:** until Marketing says otherwise, every product gets all the groups of its service line, and Marketing removes what does not apply. This needs confirming.

## Questions found in the full list (on hold)
1. **Sastrería "Dificultad jr": 205%.** Typo for 20%?
2. **Planchado SUDADERA $49 and $79:** two different garments (e.g. with/without hood)?
3. **7shoes:**
   - "Adicional" lists Repelente 10%, Suelas amarillas 20%, Suelas Ice 30%, while "Servicios especiales" lists the same items at fixed prices ($60, $129, $299). Which applies?
   - How are "Especiales" and "Restauración" ($0) charged? With the "Cuidado específico / Restaurar" tiers (BASIC, PRO, ADVANCE, FULL; upper part / lower part)?
   - Groups without step numbers (Adicional, Urgente): what order?
4. **Lavandería by the kilo:** a laundry load is not one garment. How is it labeled and tracked (one label per bag)? Is the weight typed in by the cashier?
5. **Sastrería and 7PLUS prices in lists AA, B and C:** do they exist, or is list A used everywhere for these lines?
6. **Default assignment of groups to products** (above): acceptable?
