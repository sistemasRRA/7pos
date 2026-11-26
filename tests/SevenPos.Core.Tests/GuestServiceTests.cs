using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SevenPos.Core.Guests;

namespace SevenPos.Core.Tests;

/// <summary>One test per rule in docs/features/guests.md.</summary>
public class GuestServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 26, 15, 0, 0, TimeSpan.Zero));
    private readonly GuestService _guests;

    public GuestServiceTests() => _guests = new GuestService(_database, _clock);

    public void Dispose() => _database.Dispose();

    private async Task AddGuestsAsync(params Guest[] guests)
    {
        await using var db = _database.CreateDbContext();
        db.Guests.AddRange(guests);
        await db.SaveChangesAsync();
    }

    // ---------- Register ----------

    [Fact]
    public async Task Register_ValidData_SavesNormalizedGuest()
    {
        var result = await _guests.RegisterAsync(new NewGuest("(55) 1234-5678", "  Ana   López ", " Ana@Correo.MX "));

        Assert.True(result.Succeeded);
        await using var db = _database.CreateDbContext();
        var saved = await db.Guests.SingleAsync();
        Assert.Equal("5512345678", saved.Phone);
        Assert.Equal("Ana López", saved.FullName);
        Assert.Equal("ana@correo.mx", saved.Email);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, saved.CreatedAtUtc);
    }

    [Fact]
    public async Task Register_WithConsent_RecordsWhenItWasGiven()
    {
        var result = await _guests.RegisterAsync(new NewGuest("5512345678", "Ana", MarketingConsent: true));

        Assert.True(result.Guest!.MarketingConsent);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, result.Guest.MarketingConsentAtUtc);
    }

    [Fact]
    public async Task Register_WithoutConsent_HasNoConsentDate()
    {
        var result = await _guests.RegisterAsync(new NewGuest("5512345678", "Ana"));

        Assert.False(result.Guest!.MarketingConsent);
        Assert.Null(result.Guest.MarketingConsentAtUtc);
    }

    [Fact]
    public async Task Register_InvalidData_ReturnsEveryError_AndSavesNothing()
    {
        var tomorrow = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime).AddDays(1);

        var result = await _guests.RegisterAsync(new NewGuest("123", "   ", "no-es-correo", tomorrow));

        Assert.False(result.Succeeded);
        Assert.Equal(
            [GuestError.InvalidPhone, GuestError.NameRequired, GuestError.InvalidEmail, GuestError.InvalidBirthDate],
            result.Errors);
        await using var db = _database.CreateDbContext();
        Assert.Equal(0, await db.Guests.CountAsync());
    }

    [Fact]
    public async Task Register_BirthDateBefore1900_IsInvalid()
    {
        var result = await _guests.RegisterAsync(new NewGuest("5512345678", "Ana", BirthDate: new DateOnly(1899, 12, 31)));

        Assert.Equal([GuestError.InvalidBirthDate], result.Errors);
    }

    [Fact]
    public async Task Register_PhoneAlreadyRegistered_ReturnsTheExistingGuest()
    {
        await AddGuestsAsync(new Guest { Phone = "5512345678", FullName = "Ana López" });

        var result = await _guests.RegisterAsync(new NewGuest("+52 55 1234 5678", "Otra Persona"));

        Assert.Equal([GuestError.PhoneAlreadyRegistered], result.Errors);
        Assert.Equal("Ana López", result.ExistingGuest!.FullName);
    }

    // ---------- Search ----------

    [Theory]
    [InlineData("5512345678")]
    [InlineData("55 1234 5678")]
    [InlineData("+52 55 1234 5678")]
    public async Task Search_ByFullPhone_InAnyFormat_FindsTheGuest(string query)
    {
        await AddGuestsAsync(
            new Guest { Phone = "5512345678", FullName = "Ana" },
            new Guest { Phone = "5598765432", FullName = "Beto" });

        var found = await _guests.SearchAsync(query);

        Assert.Equal(["Ana"], found.Select(g => g.FullName));
    }

    [Fact]
    public async Task Search_ByLastFourDigits_FindsMatches()
    {
        await AddGuestsAsync(
            new Guest { Phone = "5512345678", FullName = "Ana" },
            new Guest { Phone = "3398765678", FullName = "Beto" },
            new Guest { Phone = "8111112222", FullName = "Carla" });

        var found = await _guests.SearchAsync("5678");

        Assert.Equal(["Ana", "Beto"], found.Select(g => g.FullName));
    }

    [Theory]
    [InlineData("lópez")]
    [InlineData("ANA")]
    [InlineData("na ló")]
    public async Task Search_ByName_IsPartialAndCaseInsensitive(string query)
    {
        await AddGuestsAsync(
            new Guest { Phone = "5512345678", FullName = "Ana López" },
            new Guest { Phone = "5598765432", FullName = "Beto Ruiz" });

        var found = await _guests.SearchAsync(query);

        Assert.Equal(["Ana López"], found.Select(g => g.FullName));
    }

    [Fact]
    public async Task Search_IgnoresInactiveGuests()
    {
        await AddGuestsAsync(new Guest { Phone = "5512345678", FullName = "Ana", IsActive = false });

        Assert.Empty(await _guests.SearchAsync("Ana"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    [InlineData("123")]
    [InlineData("%%")]
    [InlineData("__")]
    public async Task Search_TooShortOrWildcards_FindsNothing(string query)
    {
        await AddGuestsAsync(new Guest { Phone = "5512345678", FullName = "Ana" });

        Assert.Empty(await _guests.SearchAsync(query));
    }

    [Fact]
    public async Task Search_ReturnsAtMostTwenty_SortedByName()
    {
        await AddGuestsAsync(Enumerable.Range(1, 25)
            .Select(i => new Guest { Phone = $"55000000{i:D2}", FullName = $"Cliente {i:D2}" })
            .ToArray());

        var found = await _guests.SearchAsync("Cliente");

        Assert.Equal(GuestService.MaxSearchResults, found.Count);
        Assert.Equal("Cliente 01", found[0].FullName);
        Assert.Equal("Cliente 20", found[^1].FullName);
    }
}
