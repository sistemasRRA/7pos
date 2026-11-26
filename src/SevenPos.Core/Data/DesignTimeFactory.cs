using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SevenPos.Core.Data;

/// <summary>Only used by the "dotnet ef" tool to generate migrations.</summary>
public class DesignTimeFactory : IDesignTimeDbContextFactory<PosDbContext>
{
    public PosDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PosDbContext>().UseSqlite("Data Source=design.db").Options);
}
