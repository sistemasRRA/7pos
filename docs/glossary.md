# Glossary

The client documents are in Spanish; the code is in English. Use this table to translate. In the database, names are the snake_case form of the code name (`GuestAddress` → table `guest_addresses`). **When you introduce a new concept, add it here in the same commit.**

## Entities

| Client term (Spanish) | Code | Notes |
|---|---|---|
| Invitado | `Guest` | The customer. Identified by WhatsApp phone (10 digits, see `PhoneNumber`), but the key is a UUID. Consentimiento_Marketing → `MarketingConsent`. |
| Direcciones_Invitado | `GuestAddress` | Colonia → `Neighborhood`. |
| Sucursal | `Branch` | A store, owned or franchised. `Code`: short code printed on garment labels. |
| Usuario | `User` | Staff member. Logs in with a PIN. |
| Familia | `Category` | Group of products (Camisas, Blancos, Tenis). |
| Producto | `Product` | A garment type with a base price. |
| Modificador | `Modifier` | One option of a group (Tamaño → Mediano). `ChargeType`: `Fixed` ($) or `Percentage` (% of the list price, ADR 0012). |
| Producto_Modificador_Rel | `ProductModifier` | Which modifier options a product offers. |
| Proceso / línea de servicio | `ServiceLine` | Tintorería, Planchado, Sastrería, 7shoes, 7PLUS, Lavandería. Each product belongs to one. |
| Grupo (Tamaño, Textil, Almidón…) | `ModifierGroup` | A question of the wizard. `SortOrder` = the step number (1–8) on the lists. One option per group. |
| Tickets_Cabecera | `Ticket` | An order. Folio_Interno → `Folio`; Order_ID_Publico → `PublicOrderId`; Saldo_Pendiente → `BalanceDue`. |
| Ticket_Items_Pulverizado | `Garment` | One row per physical garment. Item_Pulverizado_ID → `Code` (`BRANCH-YYYYMMDD-FOLIO-SEQ`); Precio_Lista → `ListPrice`; Precio_Cobrado → `PriceCharged`; Ubicacion_Fisica → `Location`. |
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
| Cobro anticipado (anticipo) / Liquidación | `PaymentType.Advance / Settlement`: paid when the ticket is created / paid later, usually at pickup |
| Efectivo / Tarjeta / Wallet / Liga de pago | `PaymentMethod.Cash / Card / Wallet / PaymentLink` (only cash and card accepted today) |
| Mostrador / App Logística / 7Wash / 7clean dark | `TicketOrigin.Counter / CleanCom / SevenWash / SevenCleanDark` |
| Entrada / Inicio comida / Fin comida / Salida | `AttendanceType.ClockIn / LunchStart / LunchEnd / ClockOut` |

## Business vocabulary

| Client term | Meaning | Code (when it exists) |
|---|---|---|
| Corte X | Mid-shift report that does not close the drawer. Not built: what it shows is an open question ([shifts](features/shifts.md#open-questions-for-the-client)) | `Shift` (planned) |
| Corte Z | Closing a shift: blind count, then expected vs counted cash. Also used at a shift change | `ShiftService.CloseAsync` |
| Conteo ciego | The cash counted at the close **before** seeing the expected amount | `Shift.BlindCashCount` |
| Efectivo esperado | Opening cash + cash payments − withdrawals | `ShiftSummary.ExpectedCash` |
| Sobrante / Faltante | Counted cash above / below the expected cash | `Shift.CashDifference` (+ / −) |
| Pulverizar | Splitting a ticket into one row per garment | `Garment` |
| Canon | Subscription tier of a branch; decides which modules and products it gets | `SubscriptionTier` (planned) |
| Matriz de precios | Price list for a zone of branches | `PriceList`; each dated price is a `PriceListItem` |
| MKT / Mercadotecnia | Marketing: the only role that manages items and prices | `UserRole.Marketing` |
| Cobro incremental / modelo Starbucks | Base price + upsell modifiers, asked one by one | `PriceCalculator` |
| Smart Loyalty | Points program | planned (Phase 1c) |
| BOH (back-of-house) | Production area: washing, ironing, assembly | |
| CxC / Rezagos | Receivables: ready tickets not picked up | |
| Franquiciatario | Franchise owner | |
| PROFECO | Mexican consumer protection agency; published promotion text is binding | |
| LFPDPPP | Mexican personal data protection law; requires consent for marketing | |
| CFDI | Mexican electronic invoice | |
| Desglose de IVA | Showing the IVA contained in a price that already includes it (never adding it) | [ADR 0010](decisions/0010-prices-include-iva.md) |
| Marcador | The garment's label ID, printed and scanned | `Garment.Code` |
| Folio | Internal sequential ticket number per branch | `Ticket.Folio` |
| Número de orden | The code the customer receives (8 characters, no look-alikes) | `Ticket.PublicOrderId` |
| Captura / wizard de captura | Creating a ticket at the counter, step by step | `TicketService`, `/tickets/new` |
| Urgente / Exprés | Express service, +50% of the list price | a `Modifier` |
| Lista A / AA / B / C | The price tier of the area where the branch is located (Marketing's 2026 lists) | `PriceList`, chosen per branch with `Branch.PriceListId` (Precios → Sucursales) |
| Precio sugerido | The list price in Marketing's lists | `PriceListItem.Price` |
| Política de calidad | $0 items (paid with a service, repeated cleaning); use still to be explained | |
| Facilitador | The cashier's supervisor; receives reports of missing items | |
| Depósito | Cash the branch deposits the day after the shift close | `Shift` (planned) |
