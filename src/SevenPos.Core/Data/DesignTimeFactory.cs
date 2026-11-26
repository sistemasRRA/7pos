using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SevenPos.Core.Data;

/// <summary>
/// Only used by the "dotnet ef" tool. Some commands connect to the database
/// (migrations remove, migrations list, database update), so it uses, in order:
/// SEVENPOS_DB, then the developer's user secret shared with SevenPos.Web.
/// Without either, "migrations add" still works because it never connects.
/// </summary>
public class DesignTimeFactory : IDesignTimeDbContextFactory<PosDbContext>
{
    public PosDbContext CreateDbContext(string[] args)
    {
        var secrets = new ConfigurationBuilder().AddUserSecrets<DesignTimeFactory>(optional: true).Build();
        var connection = Environment.GetEnvironmentVariable("SEVENPOS_DB")
            ?? secrets.GetConnectionString("Pos")
            ?? "Host=localhost;Database=sevenpos";
        return new(PosDatabase.CreateOptions(connection));
    }
}
