# Guests

## What it does
Cashiers find a customer ("invitado") by phone or name, or register a new one in a few seconds. The WhatsApp number identifies the customer across every branch.

## Rules

**Search** (`GuestService.SearchAsync`)
1. A **full phone number in any format** (`55 1234 5678`, `+52 55 1234 5678`…) finds that guest.
2. **4 or more digits** match anywhere in the phone. Cashiers typically type the last 4.
3. **Text of 2 or more characters** matches part of the name, ignoring upper/lower case.
4. `%` and `_` typed by the user are matched literally, not as wildcards.
5. Only active guests. At most 20 results, sorted by name.

**Register** (`GuestService.RegisterAsync`)
6. The phone is normalized to 10 digits (see `PhoneNumber`). Accepted: spaces, dashes, dots, parentheses, `+52`, and the old mobile prefix `+52 1`. Anything else is invalid.
7. The name is required. Extra spaces are removed.
8. Email is optional. If given, it is trimmed, lowercased and must look like `name@domain.tld`.
9. Birth date is optional. If given, it must be between 1900-01-01 and today.
10. All errors are returned at once, and nothing is saved.
11. A phone that already exists is rejected with the **existing guest**, so the cashier can use them instead. This also holds when two terminals register the same phone at the same moment (the database's unique index decides).
12. **Marketing consent** (LFPDPPP) is stored with the moment it was given. Without consent, no promotions may be sent.
13. `CreatedAtUtc` records when the guest was registered (for new-customer and churn reports).

## Edge cases
- **Accents matter in name search.** "Jose" does not find "José". Phone search is the main path, so this is acceptable for now. A fix is PostgreSQL's `unaccent` extension.
- Name search uses `ILIKE '%text%'`, which cannot use a normal index. That is fine for thousands of guests. With many more, add a trigram index (`pg_trgm`).
- Phone changes are not supported yet. ER rev. 2 requires OTP verification for them (Phase 1c).

## Code
- Rules: [`GuestService`](../../src/SevenPos.Core/Guests/GuestService.cs), [`PhoneNumber`](../../src/SevenPos.Core/Guests/PhoneNumber.cs)
- Tests: [`GuestServiceTests`](../../tests/SevenPos.Core.Tests/GuestServiceTests.cs) (integration), [`PhoneNumberTests`](../../tests/SevenPos.Core.Tests/PhoneNumberTests.cs) (unit)
- Screens: [`Guests.razor`](../../src/SevenPos.UI/Pages/Guests.razor), [`GuestForm.razor`](../../src/SevenPos.UI/Components/GuestForm.razor), [`GuestCard.razor`](../../src/SevenPos.UI/Components/GuestCard.razor)

## Open questions
- Editing a guest's name, email or birth date: which roles may do it?
- Must email or birth date ever be required (e.g. for birthday promotions)?
- Should search ignore accents now, or later?
- Exact consent text: does the client's legal team have approved wording?
