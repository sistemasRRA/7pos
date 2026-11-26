using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SevenPos.Core.Data;

namespace SevenPos.Core.Guests;

/// <summary>What the cashier typed in the registration form.</summary>
public record NewGuest(string? Phone, string? FullName, string? Email = null, DateOnly? BirthDate = null, bool MarketingConsent = false);

public enum GuestError { InvalidPhone, NameRequired, InvalidEmail, InvalidBirthDate, PhoneAlreadyRegistered }

public record GuestRegistration(Guest? Guest, IReadOnlyList<GuestError> Errors, Guest? ExistingGuest = null)
{
    public bool Succeeded => Guest is not null;
}

/// <summary>Search and register guests. Rules: docs/features/guests.md.</summary>
public class GuestService(IDbContextFactory<PosDbContext> dbFactory, TimeProvider clock)
{
    public const int MaxSearchResults = 20;

    private const int MinPhoneDigits = 4;
    private const int MinNameLength = 2;
    private static readonly DateOnly OldestBirthDate = new(1900, 1, 1);
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    /// <summary>
    /// Digits (4 or more) search by phone: a full number in any format finds that guest,
    /// fewer digits match anywhere in the phone (e.g. the last 4). Text (2 or more
    /// characters) searches the name, partial and case-insensitive. Active guests only.
    /// </summary>
    public async Task<List<Guest>> SearchAsync(string? query)
    {
        var text = query?.Trim() ?? "";
        await using var db = await dbFactory.CreateDbContextAsync();
        var active = db.Guests.Where(g => g.IsActive);

        IQueryable<Guest> matches;
        if (PhoneNumber.TryNormalize(text, out var fullPhone))
        {
            matches = active.Where(g => g.Phone == fullPhone);
        }
        else if (LooksLikePhone(text, out var digits))
        {
            if (digits.Length < MinPhoneDigits) return [];
            matches = active.Where(g => g.Phone.Contains(digits));
        }
        else
        {
            if (text.Length < MinNameLength) return [];
            var pattern = $"%{EscapeLikePattern(text)}%";
            matches = active.Where(g => EF.Functions.ILike(g.FullName, pattern, @"\"));
        }

        return await matches.OrderBy(g => g.FullName).Take(MaxSearchResults).AsNoTracking().ToListAsync();
    }

    public async Task<GuestRegistration> RegisterAsync(NewGuest input)
    {
        var errors = new List<GuestError>();

        if (!PhoneNumber.TryNormalize(input.Phone, out var phone)) errors.Add(GuestError.InvalidPhone);

        var name = CollapseSpaces(input.FullName);
        if (name.Length == 0) errors.Add(GuestError.NameRequired);

        var email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim().ToLowerInvariant();
        if (email is not null && !EmailPattern.IsMatch(email)) errors.Add(GuestError.InvalidEmail);

        var now = clock.GetUtcNow().UtcDateTime;
        if (input.BirthDate is { } birth && (birth > DateOnly.FromDateTime(now) || birth < OldestBirthDate))
            errors.Add(GuestError.InvalidBirthDate);

        if (errors.Count > 0) return new(null, errors);

        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.Guests.AsNoTracking().SingleOrDefaultAsync(g => g.Phone == phone);
        if (existing is not null) return new(null, [GuestError.PhoneAlreadyRegistered], existing);

        var guest = new Guest
        {
            Phone = phone,
            FullName = name,
            Email = email,
            BirthDate = input.BirthDate,
            MarketingConsent = input.MarketingConsent,
            MarketingConsentAtUtc = input.MarketingConsent ? now : null,
            CreatedAtUtc = now,
        };
        db.Guests.Add(guest);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another terminal registered the same phone between our check and our insert.
            await using var fresh = await dbFactory.CreateDbContextAsync();
            var winner = await fresh.Guests.AsNoTracking().SingleAsync(g => g.Phone == phone);
            return new(null, [GuestError.PhoneAlreadyRegistered], winner);
        }

        return new(guest, []);
    }

    /// <summary>Only digits and phone separators, e.g. "5678" or "55 1234".</summary>
    private static bool LooksLikePhone(string text, out string digits)
    {
        digits = new string(text.Where(char.IsAsciiDigit).ToArray());
        return digits.Length > 0 && text.All(c => char.IsAsciiDigit(c) || " -.()+".Contains(c));
    }

    /// <summary>Makes % and _ typed by the user match literally instead of acting as wildcards.</summary>
    private static string EscapeLikePattern(string text) =>
        text.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    private static string CollapseSpaces(string? text) =>
        string.Join(' ', (text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
