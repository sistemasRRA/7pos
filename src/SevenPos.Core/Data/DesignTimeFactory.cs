using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SevenPos.Core.Data;

/// <summary>
/// Only used by the "dotnet ef" tool. Creating a migration does not connect to the
/// database; "dotnet ef database update" does, using SEVENPOS_DB if it is set.
/// </summary>
public class DesignTimeFactory : IDesignTimeDbContextFactory<PosDbContext>
{
    public PosDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("SEVENPOS_DB") ?? PosDatabase.LocalDevConnection;
        return new(PosDatabase.CreateOptions(connection));
    }
}
