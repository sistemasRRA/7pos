using Microsoft.EntityFrameworkCore;
using SevenPos.Core.Entities;

namespace SevenPos.Core.Data;

public class PosDbContext(DbContextOptions<PosDbContext> options) : DbContext(options)
{
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<CashWithdrawal> CashWithdrawals => Set<CashWithdrawal>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<GuestAddress> GuestAddresses => Set<GuestAddress>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Modifier> Modifiers => Set<Modifier>();
    public DbSet<ProductModifier> ProductModifiers => Set<ProductModifier>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Garment> Garments => Set<Garment>();
    public DbSet<GarmentModifier> GarmentModifiers => Set<GarmentModifier>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        // Store enums as text so the database is readable ("Cash" instead of 0).
        b.Properties<Enum>().HaveConversion<string>();
        b.Properties<decimal>().HavePrecision(12, 2);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Guest>().HasIndex(x => x.Phone).IsUnique();
        b.Entity<Ticket>().HasIndex(x => new { x.BranchId, x.Folio }).IsUnique();
        b.Entity<Ticket>().HasIndex(x => x.PublicOrderId).IsUnique();
        b.Entity<Garment>().HasIndex(x => x.Code).IsUnique();
        b.Entity<ProductModifier>().HasIndex(x => new { x.ProductId, x.ModifierId }).IsUnique();
        b.Entity<Payment>().HasIndex(x => x.ShiftId);
        b.Entity<Modifier>().Property(x => x.ChargeValue).HasPrecision(12, 4);
    }

    /// <summary>Automatically flags every change as pending sync.</summary>
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        MarkChanges();
        return base.SaveChangesAsync(ct);
    }

    public override int SaveChanges()
    {
        MarkChanges();
        return base.SaveChanges();
    }

    private void MarkChanges()
    {
        var now = DateTime.UtcNow;
        foreach (var e in ChangeTracker.Entries<SyncEntity>())
        {
            if (e.State is EntityState.Added or EntityState.Modified)
            {
                e.Entity.LastModifiedUtc = now;
                e.Entity.SyncStatus = SyncStatus.Pending;
            }
        }
    }
}
