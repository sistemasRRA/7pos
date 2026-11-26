# PIN login

## What it does
Staff log in on the branch terminal by tapping their name and entering a 4-digit PIN. Repeated wrong PINs lock the user for a few minutes, and every attempt is written to the audit log.

## Rules
1. The login screen lists the **active** users of the terminal's branch, sorted by name.
2. A correct PIN logs the user in.
3. A wrong PIN is rejected, and the screen says how many attempts are left.
4. The **3rd wrong PIN in a row locks the user for 5 minutes**. While locked, even the correct PIN is rejected.
5. After the lock expires the user gets 3 fresh attempts.
6. A successful login resets the failure counter.
7. Inactive users (`IsActive = false`) cannot log in.
8. `LOGIN`, `LOGIN_FAILED` and `USER_LOCKED` are written to `AuditLog` with user, branch and time.
9. PINs are stored only as salted PBKDF2 hashes (`PinHasher`), never in plain text.
10. Every screen except `/login` uses `PosLayout`, which sends users without a session back to `/login`.

Limits are constants in `LoginService`: `MaxFailedAttempts = 3`, `LockoutDuration = 5 min`.

## Edge cases
- **Terminal branch:** until device registration exists (DISPOSITIVOS in ER rev. 2), the terminal uses the first active branch by name. With several branches, every terminal shows the same branch's users.
- **Refreshing the browser logs you out.** The session lives in memory for one browser connection (Blazor Server circuit). This is acceptable for a shared terminal.
- **Prerendering is off** (`App.razor`). With prerendering, Blazor renders each page twice in two different scopes; the first render would see no session and always redirect to `/login`.
- Two terminals entering wrong PINs for the same user at the same instant could miss one count. This is acceptable for now.

## Code
- Rules: [`LoginService`](../../src/SevenPos.Core/Organization/LoginService.cs), [`PinHasher`](../../src/SevenPos.Core/Organization/PinHasher.cs), [`PosSession`](../../src/SevenPos.Core/Organization/PosSession.cs)
- Tests: [`LoginServiceTests`](../../tests/SevenPos.Core.Tests/LoginServiceTests.cs). They use `FakeTimeProvider` to move time forward without waiting.
- Screens: [`Login.razor`](../../src/SevenPos.UI/Pages/Login.razor), [`PinPad.razor`](../../src/SevenPos.UI/Components/PinPad.razor), [`PosLayout.razor`](../../src/SevenPos.UI/Layout/PosLayout.razor)

## Open questions
- **Automatic logout after inactivity** (the brief requires it): how many minutes?
- Should a manager be able to unlock a user before the 5 minutes are up?
- PIN length: always 4 digits, or configurable per branch?
