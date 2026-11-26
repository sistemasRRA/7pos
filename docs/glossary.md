# Glossary

The client documents are in Spanish; the code is in English. Use this table to translate. **When you introduce a new concept, add it here in the same commit.**

## Entities

| Client term (Spanish) | Code | Notes |
|---|---|---|
| Invitado | `Guest` | The customer. Identified by WhatsApp phone, but the key is a UUID. |
| Direcciones_Invitado | `GuestAddress` | Colonia → `Neighborhood`. |
| Sucursal | `Branch` | A store, owned or franchised. |
| Usuario | `User` | Staff member. Logs in with a PIN. |
| Familia | `Category` | Group of products (Camisas, Blancos, Tenis). |
| Producto | `Product` | A garment type with a base price. |
| Modificador | `Modifier` | Paid extra on a garment (Almidón, Express). `ChargeType`: `Fixed` ($) or `Percentage` (%). |
| Producto_Modificador_Rel | `ProductModifier` | Which modifiers a product allows. |
| Tickets_Cabecera | `Ticket` | An order. Folio_Interno → `Folio`; Order_ID_Publico → `PublicOrderId`; Saldo_Pendiente → `BalanceDue`. |
| Ticket_Items_Pulverizado | `Garment` | One row per physical garment. Item_Pulverizado_ID → `Code`; Ubicacion_Fisica → `Location`. |
| Ticket_Items_Modificadores | `GarmentModifier` | Extras sold on that exact garment. Cargo_Aplicado → `ChargeApplied`. |
| Transacciones_Pagos | `Payment` | Naturaleza → `Type`; Metodo_Pago → `Method`. |
| Cajas_Turnos | `Shift` | A cashier shift. Fondo_Inicial → `OpeningCash`. |
| Declaración ciega | `BlindCashCount` | Cash counted before seeing the expected amount. |
| Faltante / Sobrante | `CashDifference` | Negative = short, positive = over. |
| Salidas de efectivo | `CashWithdrawal` | Cash taken out of the drawer (supplier, expenses, pickups). |
| Asistencias_RRHH | `Attendance` | Time clock entries. |
| Audit_Log / Bitácora | `AuditLog` | |

## Enum values

| Client term | Code |
|---|---|
| Recibido / En Proceso / Listo / En Ruta / Entregado | `TicketStatus.Received / InProcess / Ready / OutForDelivery / Delivered` |
| Sucio / Lavado / Planchado / Armado / Entregado | `GarmentStatus.Dirty / Washed / Ironed / Assembled / Delivered` (plus `InTransit`) |
| Cobro anticipado / Liquidación | `PaymentType.Advance / Settlement` |
| Efectivo / Tarjeta / Wallet / Liga de pago | `PaymentMethod.Cash / Card / Wallet / PaymentLink` |
| Mostrador / App Logística / 7Wash / 7clean dark | `TicketOrigin.Counter / CleanCom / SevenWash / SevenCleanDark` |
| Entrada / Inicio comida / Fin comida / Salida | `AttendanceType.ClockIn / LunchStart / LunchEnd / ClockOut` |

## Business vocabulary

| Client term | Meaning | Code (when it exists) |
|---|---|---|
| Corte X | Mid-day shift report (shift change), does not close the day | `Shift` |
| Corte Z | End-of-day close | `Shift` |
| Pulverizar | Splitting a ticket into one row per garment | `Garment` |
| Canon | Subscription tier of a branch; decides which modules and products it gets | `SubscriptionTier` (planned) |
| Matriz de precios | Price list for a zone of branches | `PriceList` (planned) |
| Cobro incremental / modelo Starbucks | Base price + upsell modifiers, asked one by one | `PriceCalculator` |
| Smart Loyalty | Points program | planned (Phase 1c) |
| BOH (back-of-house) | Production area: washing, ironing, assembly | |
| CxC / Rezagos | Receivables: ready tickets not picked up | |
| Franquiciatario | Franchise owner | |
| PROFECO | Mexican consumer protection agency; published promotion text is binding | |
| LFPDPPP | Mexican personal data protection law; requires consent for marketing | |
| CFDI | Mexican electronic invoice | |
