# Client answers · 2026-11-26

Answers to the open questions in the [roadmap](../roadmap.md). The client's original text is kept verbatim (Spanish) as the source; each answer is followed by our interpretation and the follow-ups it raised.

## Original text

> 1) nosotros ya tenemos el IVA incluído en el precio final, solo hay que hacer el desglose, por ejemplo si la prenda cuesta $100, NO SUMAMOS, DESGLOSAMOS
>
> Subtotal (Base) = $100 / 1.16= $86.21
> IVA (16%)=  $86.21 x  0.16 = $13.79
> Total  $100.00
>
> El sistema ya debe de tener ese cálculo para hacer el desglose
>
> 2) No pueden crear ítems ni pueden cambiar precios (la gente abusa y hacen una fiesta, es un problema con el que sigo lidiando)
> Si no está el item deberán elegir algo del mismo precio o similar, reportarlo con su facilitaron y solo MKT puede crear o eliminar ítems con el control de la lista de precios
>
> 3) Los retiros en efectivo se reflejan en el cierre de caja, indica cuánto deben de depositar al día siguiente para entregar las cuentas
>
> 4) En el cambio de estatus guardamos el ticket, cliente y el ID del marcador (acuérdate que es por prenda para identificarla)
> Si quieres mañana te explico mejor lo del historial de cambios de estatus
> Ese es un poco más rebuscado pero no difícil, es para saber que prendas y pedidos tenemos en la tienda a la hora de hacer un inventario, para hacer identificación de prendas y seguimientos, y para poder entregar y avisar al cliente que ya está listo su pedido

## Interpretation

### 1. IVA: prices already include it → [ADR 0010](../decisions/0010-prices-include-iva.md)
The price shown is the final price. The system never adds IVA; it **breaks it down** on tickets and reports: subtotal = total / 1.16, IVA = the difference.

**Follow-ups:**
- Confirm the calculation that always adds up to the cent: `Subtotal = round(Total / 1.16, 2)`, `IVA = Total − Subtotal`. Their $100 example gives the same result.
- Break down per ticket (recommended) or per garment? The two can differ by cents.
- Any branch in the border zone (8%)?
- Do guests ask for invoices (CFDI)? Not answered.

### 2. Only Marketing creates items and changes prices → [ADR 0011](../decisions/0011-only-marketing-controls-catalog-and-prices.md)
Cashiers cannot create items or change prices; this has been abused. If an item is missing, the cashier picks one of the same or similar price and reports it to their supervisor ("facilitador"). Only Marketing creates or removes items, through the price lists.

This answers the old "missing price" question: a product without a price in the branch's list **is not offered** for sale.

**Follow-up:**
- Are **manual discounts** allowed at all (ER rev. 2 suggests a supervisor PIN), or forbidden like price changes?

### 3. Cash withdrawals appear in the shift close
The close report shows the withdrawals and **how much cash must be deposited the next day**.

**Follow-ups:**
- Deposit formula: cash collected − withdrawals? Does the opening float stay in the drawer?
- Withdrawal fields: reason, who authorizes, signature or receipt?

### 4. Garment status history: explanation pending
Each status change records the ticket, the guest and the garment's label ID ("marcador", our `Garment.Code`; status is per garment). It is used for in-store inventory, garment identification, tracking, and telling the customer the order is ready. The client offered to explain it in detail.

**Ask during that explanation:**
- Who made the change, when, from which status to which?
- Is the physical location recorded too ("Rack 2", "Casillero A3")?
- Is the change scanned per garment, or applied to a whole ticket?
- At which status is the customer notified?
