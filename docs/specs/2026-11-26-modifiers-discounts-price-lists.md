# Client answers · 2026-11-26 (second round)

Answers to the two questions that blocked the ticket wizard (modifier order and manual discounts), plus Marketing's 2026 price lists. The client's text is kept verbatim (Spanish), followed by our interpretation.

## Original text

On Express (+50%):

> Por ejemplo, la camisa vale 100, pero ya le pusiste un incremento del 10% de almidón, entonces no va calcular el servicio exprés sobre los 110 que ya lleva por su almidón, sino que calcula el servicio exprés sobre los 100 que costaba la prenda sin los servicios, entonces te debería quedar la prenda en 160

On discounts (relayed by the developer): discounts are only possible when there is a promotion; the user cannot give a discount manually.

Price lists: four PDFs from Marketing, `MKT Lista Precios 2026 A / AA / B / C Simplificada.pdf`, transcribed to:
- [2026-11-26-price-lists-2026.csv](2026-11-26-price-lists-2026.csv): group, product and price in each list
- [2026-11-26-modifiers-2026.csv](2026-11-26-modifiers-2026.csv): modifier groups, options, percentages and the lists that show them

## Interpretation

### 1. Percentages apply to the list price → [ADR 0012](../decisions/0012-percentage-modifiers-on-list-price.md)
Every percentage extra is calculated on the garment's price **without services**. Extras add up and never compound: $100 + Almidón 10% ($10) + Express 50% ($50) = **$160**. The order of the extras no longer changes the price.

### 2. No manual discounts → [ADR 0013](../decisions/0013-no-manual-discounts.md)
Nobody types a discount at the counter, not even with a supervisor PIN. A ticket gets a discount only from a promotion or coupon (roadmap 1b). Until then, `Discounts = 0`.

### 3. The 2026 price lists
What the PDFs show:
- **Four lists:** A, AA, B and C. Each product has one price per list (e.g. CAMISA: A $89, AA $99, B $79, C $69).
- **59 products in 7 groups:** Ropa de vestir / Casual, Ropa de cama y blancos, Accesorios, Especiales y misceláneo, Hogar y decoración, Toallas y baño, Política de calidad.
- **Six products are not in list A:** SLEEPING, COBERTOR, COLCHA, CORTINA, EDREDON, CUBRE COLCHÓN.
- **Two $0 products under "Política de calidad":** PRENDA PAGO CON SERVICIO and REPETICIÓN DE LIMPIEZA PRENDA.
- **Every extra is a percentage**, organized in **groups with options**, numbered 1–8 in lists AA, B and C:

  | # | Group | Options |
  |---|---|---|
  | 1 | Tamaño | Mediano 15%, Grande / Largo 20%, Extra largo 30%, Matrimonial 15%, Queen / King 20% |
  | 2 | Textil | Lino 15%, Seda 20%, Vini piel 25% |
  | 3 | Suciedad | Media 15%, Pesada 20% |
  | 4 | Relleno | Seda 10%, Pluma 20% |
  | 5 | Espesor | Medio 15%, Grueso 20%, Extra grueso 25% |
  | 6 | Almidón | Ligero 5%, Medio 10%, Rígido 15% |
  | 7 | Extra | Aplicaciones 30%, Prenda delicada 50% |
  | 8 | Urgente | Urgente 50% |

- **List A has no Tamaño or Relleno.** The other percentages are the same in all four lists.

**What this changes in the model:** today a modifier is a single extra with a fixed or percentage charge, linked to products one by one. The lists need **modifier groups** where the cashier picks at most one option per group, products with a $0 price, and possibly extras that depend on the list. This is planned as the next feature ("real catalog").

**Follow-ups for the client:**
1. **Order of the questions:** are the numbers 1–8 the order the wizard asks (Tamaño, Textil, Suciedad, Relleno, Espesor, Almidón, Extra, Urgente)?
2. **One option per group?** E.g. can a garment be both "Aplicaciones" and "Prenda delicada" (Extra), or only one?
3. **Which groups apply to which products?** Does a CORBATA get asked about Tamaño and Relleno, or only some groups per product or product group?
4. **List A without Tamaño and Relleno:** intended, or is the A sheet older than the others?
5. **The $0 products:** what are "PRENDA PAGO CON SERVICIO" and "REPETICIÓN DE LIMPIEZA PRENDA" used for? Do extras apply to them? Who may use them?
6. **Which branches use which list?** (A, AA, B, C)
7. **"Precio sugerido":** can a franchise change these prices, or are they fixed by Marketing (ADR 0011)?
