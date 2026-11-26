using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SevenPos.Core.Data;

/// <summary>
/// Only used by the "dotnet ef" tool. Creating a migration does not connect to the
/// database, so no credentials are needed; commands that do connect (such as
/// "dotnet ef database update") read SEVENPOS_DB. The app applies migrations itself on startup.
/// </summary>
public class DesignTimeFactory : IDesignTimeDbContextFactory<PosDbContext>
{
    public PosDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("SEVENPOS_DB") ?? "Host=localhost;Database=sevenpos";
        return new(PosDatabase.CreateOptions(connection));
    }
}
