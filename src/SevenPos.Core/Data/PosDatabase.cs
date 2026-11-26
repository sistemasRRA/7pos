using Microsoft.EntityFrameworkCore;

namespace SevenPos.Core.Data;

/// <summary>
/// The single place where the database connection is configured. The web app,
/// the tests and the "dotnet ef" tool all call this, so they can never disagree.
/// </summary>
public static class PosDatabase
{
    public const string LocalDevConnection =
        "Host=localhost;Port=5432;Database=sevenpos;Username=sevenpos;Password=sevenpos_dev";

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
