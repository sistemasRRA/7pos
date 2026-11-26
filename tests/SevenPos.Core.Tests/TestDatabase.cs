using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        var server = new NpgsqlConnectionStringBuilder(ServerConnectionString());
        _serverConnectionString = new NpgsqlConnectionStringBuilder(server.ConnectionString) { Database = "postgres" }.ConnectionString;
        _connectionString = new NpgsqlConnectionStringBuilder(server.ConnectionString) { Database = _name }.ConnectionString;
        _options = PosDatabase.CreateOptions(_connectionString);

        using var db = CreateDbContext();
        db.Database.Migrate();
    }

    public PosDbContext CreateDbContext() => new(_options);

    /// <summary>
    /// The server to create test databases on: SEVENPOS_TEST_DB if set (e.g. in CI),
    /// otherwise the developer's local connection string from user secrets, shared with
    /// SevenPos.Web. The database name in it is ignored; each test gets its own.
    /// </summary>
    private static string ServerConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("SEVENPOS_TEST_DB");
        if (!string.IsNullOrWhiteSpace(fromEnvironment)) return fromEnvironment;

        var secrets = new ConfigurationBuilder().AddUserSecrets<TestDatabase>(optional: true).Build();
        return secrets.GetConnectionString("Pos") ?? throw new InvalidOperationException(
            "No database configured for the tests. Set it once with: dotnet user-secrets set " +
            "\"ConnectionStrings:Pos\" \"Host=localhost;Port=5432;Database=sevenpos;Username=sevenpos;Password=<your password>\" " +
            "--project src/SevenPos.Web (or set SEVENPOS_TEST_DB).");
    }

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
