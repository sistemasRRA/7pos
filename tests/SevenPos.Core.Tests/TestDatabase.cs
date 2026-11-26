using Microsoft.EntityFrameworkCore;
using Npgsql;
using SevenPos.Core.Data;

namespace SevenPos.Core.Tests;

/// <summary>
/// A throwaway PostgreSQL database for one test: created and migrated on construction,
/// dropped on Dispose. Also works as the IDbContextFactory that services expect.
/// Point it at another server with the SEVENPOS_TEST_DB environment variable.
/// </summary>
public sealed class TestDatabase : IDbContextFactory<PosDbContext>, IDisposable
{
    private readonly DbContextOptions<PosDbContext> _options;

    public TestDatabase()
    {
        var server = Environment.GetEnvironmentVariable("SEVENPOS_TEST_DB") ?? PosDatabase.LocalDevConnection;
        var connection = new NpgsqlConnectionStringBuilder(server) { Database = $"sevenpos_test_{Guid.NewGuid():N}" };
        _options = PosDatabase.CreateOptions(connection.ConnectionString);
        using var db = CreateDbContext();
        db.Database.Migrate();
    }

    public PosDbContext CreateDbContext() => new(_options);

    public void Dispose()
    {
        using var db = CreateDbContext();
        db.Database.EnsureDeleted();
    }
}
