using Microsoft.EntityFrameworkCore;

namespace SevenPos.Core.Data;

/// <summary>
/// The single place where the database connection is configured. The web app,
/// the tests and the "dotnet ef" tool all call this, so they can never disagree.
/// Connection strings are never stored in code: see docs/getting-started.md.
/// </summary>
public static class PosDatabase
{
    public static void Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString)
            // C# keeps PascalCase (ticket.BranchId); the database uses PostgreSQL's
            // snake_case (tickets.branch_id), so hand-written SQL needs no quotes.
            .UseSnakeCaseNamingConvention();

    public static DbContextOptions<PosDbContext> CreateOptions(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<PosDbContext>();
        Configure(builder, connectionString);
        return builder.Options;
    }
}
