# Payments (advance and settlement)

## What it does
The customer can pay **part or all** of a ticket when it is created (an **advance**, cobro anticipado), and the rest later, usually at pickup (a **settlement**, liquidación), from the **Cobrar** page. Today the branch accepts **cash and card**. Every payment comes into the branch's **open shift**, so it shows up in that shift's close (Corte Z), and the ticket's **balance** always equals its total minus what was paid.

## Rules

**Any payment** (`PaymentService`, `TicketService`)
1. Only branch staff (Cashier, Manager, Admin) take payments; Marketing cannot. The role is read from the database.
2. Accepted methods: **cash and card**. Wallet and payment link arrive in Phase 3 and are refused.
3. An amount is more than $0, in whole cents (no $10.005). The database also refuses $0 or less (`ck_payments_amount_positive`).
4. A payment needs the branch's **open shift** and is recorded on it, with who received it and when.
5. A payment lowers `Ticket.BalanceDue` **in the same transaction**, so the balance never disagrees with the payments.
6. Each payment is audited as `PAYMENT_RECEIVED` (ticket, folio, amount, method, type, balance after).

**When the ticket is created: advance** (`TicketService.CreateAsync`, `NewTicket.Advance`)
7. Optional: any amount up to the ticket's **total** (paying everything is allowed). Without an advance the customer owes the whole total and no shift is needed.
8. The ticket, its advance and the shift lock are **one transaction**: if the advance is refused (no open shift, above the total…), nothing is saved.
9. Its type is `Advance`.

**Later: settlement** (`PaymentService.PayAsync`, page Cobrar)
10. The cashier finds the ticket by **order number** (any case), **folio** (with or without zeros) or the **customer's phone** (`TicketService.FindAsync`), only among the branch's own tickets.
11. Any amount up to the **balance**. Its type is `Settlement`.
12. Tickets of another branch and cancelled tickets cannot be paid.

**Cash and change** (page only)
13. For cash, the cashier may type what the customer hands over; the screen shows the **change**. Only the amount paid is recorded and counted in the drawer, never the cash received.

**Concurrency**
14. Two terminals paying the same ticket run one after the other (the ticket row is locked `FOR UPDATE`), so together they can never pay more than the balance.
15. A payment holds the shift row `FOR SHARE`; the close (`FOR UPDATE`) waits for it, and a payment that waited for a close finds no open shift. No payment can land on a closed shift.

## Edge cases
- **Drawer closed in the wizard:** the payment section says so and starts on "Sin pago ahora"; the ticket can still be created without a payment.
- **Prices changed between the preview and saving** so the advance is now above the total: "El anticipo es mayor que el total del ticket."
- **Someone paid the ticket on another terminal:** "El monto es mayor que el saldo…", and the page reloads the balance.
- **Fully paid ticket:** Cobrar shows "Pagado por completo" and no payment form.
- **An all-digit order number** (possible: "23456789") is searched as a folio and as an order number.

## Code
- Model: [`Payment`](../../src/SevenPos.Core/Cash/Payment.cs), [`Ticket`](../../src/SevenPos.Core/Tickets/Ticket.cs) (`BalanceDue`)
- Rules: [`PaymentService`](../../src/SevenPos.Core/Cash/PaymentService.cs), [`TicketService`](../../src/SevenPos.Core/Tickets/TicketService.cs) (advance, `FindAsync`)
- Tests: [`PaymentServiceTests`](../../tests/SevenPos.Core.Tests/PaymentServiceTests.cs), [`TicketServiceTests`](../../tests/SevenPos.Core.Tests/TicketServiceTests.cs), [`DatabaseTests`](../../tests/SevenPos.Core.Tests/DatabaseTests.cs)
- Screens: [`CollectPayment.razor`](../../src/SevenPos.UI/Cash/CollectPayment.razor) (`/pay`, "Cobrar"), [`PaymentForm`](../../src/SevenPos.UI/Cash/PaymentForm.razor), [`PaymentList`](../../src/SevenPos.UI/Cash/PaymentList.razor), [`TicketWizard.razor`](../../src/SevenPos.UI/Tickets/TicketWizard.razor), [`TicketDetails`](../../src/SevenPos.UI/Tickets/TicketDetails.razor)

## Not built yet
- **Delivery (pickup):** marking garments as delivered, and whether a ticket must be fully paid first (garment lifecycle, roadmap step 8).
- Refunds and cancelling a payment (and who authorizes them).
- Card details (terminal, last 4 digits, authorization number).
- Wallet and payment links (Phase 3); discounts from promotions (1b).
- Printing the ticket with its payments.

## Open questions (for the client)
1. Is there a **minimum advance** (for example 50%), or can a ticket leave with $0 paid? Today any amount is allowed.
2. Must the ticket be **paid in full before delivery**?
3. Is a payment made after drop-off but **before pickup** a "cobro anticipado" or a "liquidación"? Today: advance only at creation, settlement afterwards.
4. For card: do we record the terminal's **authorization number** or the last 4 digits?
5. Can a ticket be **paid in another branch** (franchise clearing is Phase 3)? Today: only in its own branch.
