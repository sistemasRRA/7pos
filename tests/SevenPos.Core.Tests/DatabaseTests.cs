using Microsoft.EntityFrameworkCore;
using Npgsql;
using SevenPos.Core.Data;
using SevenPos.Core.Catalog;
using SevenPos.Core.Common;
using SevenPos.Core.Guests;
using SevenPos.Core.Organization;
using SevenPos.Core.Tickets;

namespace SevenPos.Core.Tests;

/// <summary>Integration tests: the database itself must protect the data.</summary>
public class DatabaseTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly PosDbContext _db;

    public DatabaseTests() => _db = _database.CreateDbContext();

    public void Dispose()
    {
        _db.Dispose();
        _database.Dispose();
    }

    [Fact]
    public async Task SeedData_LoadsCatalogAndUser_OnlyOnce()
    {
        await SeedData.LoadAsync(_db);
        await SeedData.LoadAsync(_db);

        Assert.Equal(1, await _db.Branches.CountAsync());
        Assert.True(await _db.Products.CountAsync() > 5);
        var admin = await _db.Users.SingleAsync(u => u.Role == UserRole.Admin);
        Assert.True(PinHasher.Verify("1234", admin.PinHash));
        Assert.False(PinHasher.Verify("0000", admin.PinHash));
    }

    [Fact]
    public async Task SeedData_GivesTheBranchAPriceList_AndAMarketingUser()
    {
        await SeedData.LoadAsync(_db);

        var branch = await _db.Branches.SingleAsync();
        Assert.NotNull(branch.PriceListId);
        Assert.Equal(await _db.Products.CountAsync(p => p.IsActive), await _db.PriceListItems.CountAsync());
        var marketing = await _db.Users.SingleAsync(u => u.Role == UserRole.Marketing);
        Assert.True(PinHasher.Verify("5678", marketing.PinHash));
    }

    [Fact]
    public async Task SeedData_CompletesADatabaseFromAnOlderVersion()
    {
        // An older version seeded the branch and catalog, but had no price lists or Marketing role.
        var branch = new Branch { Name = "Sucursal Demo", Code = "DEMO" };
        var category = new Category { Name = "Camisas" };
        _db.Branches.Add(branch);
        _db.Categories.Add(category);
        _db.Products.Add(new Product { Name = "Camisa de vestir", BasePrice = 50, CategoryId = category.Id });
        await _db.SaveChangesAsync();

        await SeedData.LoadAsync(_db);

        Assert.Equal(1, await _db.Branches.CountAsync());
        Assert.NotNull((await _db.Branches.SingleAsync()).PriceListId);
        Assert.Equal(50m, (await _db.PriceListItems.SingleAsync()).Price);
    }

    [Fact]
    public async Task Saving_MarksRecordAsPendingSync()
    {
        var guest = new Guest { Phone = "5512345678", FullName = "Prueba", SyncStatus = SyncStatus.Synced };
        _db.Guests.Add(guest);
        await _db.SaveChangesAsync();
        Assert.Equal(SyncStatus.Pending, guest.SyncStatus);
    }

    [Fact]
    public async Task GuestPhone_IsUnique()
    {
        _db.Guests.Add(new Guest { Phone = "5500000000", FullName = "A" });
        _db.Guests.Add(new Guest { Phone = "5500000000", FullName = "B" });
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task Ticket_WithBranchThatDoesNotExist_IsRejected()
    {
        await SeedData.LoadAsync(_db);
        var guest = new Guest { Phone = "5511111111", FullName = "Cliente" };
        _db.Guests.Add(guest);
        _db.Tickets.Add(new Ticket
        {
            Folio = 1, PublicOrderId = "X1", GuestId = guest.Id,
            BranchId = Guid.NewGuid(),                     // no such branch
            CashierId = (await _db.Users.FirstAsync()).Id,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task Branch_WithUsers_CannotBeDeleted()
    {
        await SeedData.LoadAsync(_db);

        // Forget everything EF has in memory (the seeded users), so EF sends the
        // DELETE and it is PostgreSQL itself that has to refuse it.
        _db.ChangeTracker.Clear();
        _db.Branches.Remove(await _db.Branches.FirstAsync());

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
        var postgresError = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgresError.SqlState); // 23503
    }

    [Fact]
    public async Task Money_KeepsExactCents()
    {
        await SeedData.LoadAsync(_db);
        var product = await _db.Products.FirstAsync();
        product.BasePrice = 0.1m + 0.2m;
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();
        Assert.Equal(0.30m, (await _db.Products.FindAsync(product.Id))!.BasePrice);
    }
}
