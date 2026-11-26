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
    private readonly string _name = $"sevenpos_test_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private readonly string _serverConnectionString;
    private readonly DbContextOptions<PosDbContext> _options;

    public TestDatabase()
    {
        var server = new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("SEVENPOS_TEST_DB") ?? PosDatabase.LocalDevConnection);
        _serverConnectionString = new NpgsqlConnectionStringBuilder(server.ConnectionString) { Database = "postgres" }.ConnectionString;
        _connectionString = new NpgsqlConnectionStringBuilder(server.ConnectionString) { Database = _name }.ConnectionString;
        _options = PosDatabase.CreateOptions(_connectionString);

        using var db = CreateDbContext();
        db.Database.Migrate();
    }

    public PosDbContext CreateDbContext() => new(_options);

    /// <summary>
    /// Drops the database with a plain DROP DATABASE. EF Core's EnsureDeleted uses
    /// DROP DATABASE ... WITH (FORCE), which tries to kill every session on the database.
    /// When PostgreSQL's autovacuum worker happens to be connected, that fails with
    /// 42501 (our role may not kill a superuser process) and the test fails at random.
    /// A plain DROP lets PostgreSQL stop autovacuum itself.
    /// </summary>
    public void Dispose()
    {
        // Close this database's pooled connections so they do not block the DROP.
        using (var pooled = new NpgsqlConnection(_connectionString))
            NpgsqlConnection.ClearPool(pooled);

        using var server = new NpgsqlConnection(_serverConnectionString);
        server.Open();
        using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_name}\"", server);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                drop.ExecuteNonQuery();
                return;
            }
            catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.ObjectInUse && attempt < 5)
            {
                Thread.Sleep(200); // another session is still closing; try again
            }
        }
    }
}
