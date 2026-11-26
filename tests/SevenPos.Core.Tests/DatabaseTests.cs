using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SevenPos.Core.Data;
using SevenPos.Core.Entities;
using SevenPos.Core.Services;

namespace SevenPos.Core.Tests;

public class DatabaseTests : IDisposable
{
    private readonly SqliteConnection _conn = new("Data Source=:memory:");
    private readonly PosDbContext _db;

    public DatabaseTests()
    {
        _conn.Open();
        _db = new PosDbContext(new DbContextOptionsBuilder<PosDbContext>().UseSqlite(_conn).Options);
        _db.Database.Migrate();
    }

    public void Dispose() { _db.Dispose(); _conn.Dispose(); }

    [Fact]
    public async Task SeedData_LoadsCatalogAndUser_OnlyOnce()
    {
        await SeedData.LoadAsync(_db);
        await SeedData.LoadAsync(_db);

        Assert.Equal(1, await _db.Branches.CountAsync());
        Assert.True(await _db.Products.CountAsync() > 5);
        var admin = await _db.Users.SingleAsync();
        Assert.True(PinHasher.Verify("1234", admin.PinHash));
        Assert.False(PinHasher.Verify("0000", admin.PinHash));
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
}
