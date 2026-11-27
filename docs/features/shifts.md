# Shifts (cash drawer: open, withdrawals, Corte Z)

## What it does
Each branch has one cash drawer. A cashier **opens the shift** with the cash already in the drawer (fondo inicial), records every **withdrawal** (cash taken out for suppliers, expenses or pickups), and **closes the shift (Corte Z)** by counting the cash **blind**, without first seeing how much should be there. The close then shows the expected cash, the counted cash and the difference (sobrante / faltante).

Payments (the next feature) will require an open shift, so every peso that comes in belongs to one shift and shows up in its close.

Source: ER rev. 2 (`Cajas_Turnos`, `Salidas de efectivo`) and the [client answers](../specs/2026-11-26-client-answers.md#3-cash-withdrawals-appear-in-the-shift-close).

## Rules

**Who** (`ShiftService`)
1. Any active branch user (Cashier, Manager, Admin) can open, withdraw from and close the drawer of **their own branch**. Marketing cannot. The role is read from the database, never trusted from the screen.

**Opening**
2. The opening cash is $0 or more.
3. A branch has **at most one open shift**. The database enforces it with a filtered unique index (`ix_shifts_open_per_branch`), so two terminals opening at the same moment cannot both succeed.

**Withdrawals**
4. A withdrawal needs an open shift, an amount greater than $0 and a reason. It records who took the cash and when.
5. A withdrawal can never be more than the cash the drawer should have. Card, Wallet and payment-link money is not in the drawer.
6. Two withdrawals (or a withdrawal and the close) on the same drawer run one after the other: each one locks the shift row (`SELECT … FOR UPDATE`) before checking the cash.

**Close (Corte Z)**
7. **Expected cash = opening cash + cash payments − withdrawals**, always computed from the rows, never stored as a running total ([ADR 0007](../decisions/0007-store-facts-derive-totals.md)).
8. The cashier types the **counted cash first**. While the shift is open the Caja page never shows the expected cash, so the count stays blind.
9. The close stores the count (`BlindCashCount`), the difference (`CashDifference` = counted − expected; negative = short), who closed it and when. After closing, a new shift can be opened.
10. The close report shows: opening cash, cash payments, withdrawals, expected cash, counted cash, the difference, payments per method and the list of withdrawals.

**Audit**
11. `SHIFT_OPENED` (opening cash), `CASH_WITHDRAWN` (amount, reason) and `SHIFT_CLOSED` (expected, counted, difference).

**Tickets**
12. A new ticket belongs to the branch's open shift (`Ticket.ShiftId`). Without an open shift it has none for now; payments will require one.

## Edge cases
- **Shift change during the day:** the outgoing cashier closes (Corte Z) and the incoming one opens a new shift with the cash they find.
- **Someone else closed the drawer** while the page was open: the next action says "La caja ya está cerrada." and the page reloads.
- **Two terminals open at once:** one gets "Alguien más ya abrió la caja de esta sucursal."
- **The withdrawal error hints at the expected cash** ("El retiro es mayor que el efectivo que debe haber en la caja."). That is the price of rule 5; the exact amount is never shown.
- There are no payments yet, so today the expected cash is the opening cash minus the withdrawals.

## Code
- Model: [`Shift`](../../src/SevenPos.Core/Cash/Shift.cs), [`CashWithdrawal`](../../src/SevenPos.Core/Cash/CashWithdrawal.cs), [`Payment`](../../src/SevenPos.Core/Cash/Payment.cs)
- Rules: [`ShiftService`](../../src/SevenPos.Core/Cash/ShiftService.cs), [`TicketService`](../../src/SevenPos.Core/Tickets/TicketService.cs) (shift link)
- Tests: [`ShiftServiceTests`](../../tests/SevenPos.Core.Tests/ShiftServiceTests.cs), [`TicketServiceTests`](../../tests/SevenPos.Core.Tests/TicketServiceTests.cs), [`DatabaseTests`](../../tests/SevenPos.Core.Tests/DatabaseTests.cs)
- Screens: [`CashDrawer.razor`](../../src/SevenPos.UI/Cash/CashDrawer.razor) (`/cash`, "Caja"), [`Withdrawals.razor`](../../src/SevenPos.UI/Cash/Withdrawals.razor)

## Not built yet
- **Corte X** (see the open questions).
- **The deposit:** how much cash to take to the bank the next day.
- Printing the close report and emailing it to `Branch.AlertEmails`.
- A list of past closes for managers.

## Open questions (for the client)
1. **Deposit formula:** is it counted cash − opening cash (the opening cash stays in the drawer for the next day), or all of the counted cash?
2. **Corte X:** is it a mid-shift report that does not close the drawer? If it shows the expected cash, the cashier sees it before the blind count. Should only a Manager see it?
3. **Withdrawals:** does someone have to authorize them (Manager PIN)? Is a signature or receipt photo needed? Is there a maximum?
4. **Who can close:** any cashier of the branch, or only the one who opened the shift (or a Manager)?
5. **Differences:** above what amount should a short or over be reported (for example by email to `AlertEmails`)?
