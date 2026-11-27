using Microsoft.EntityFrameworkCore;

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
    public DbSet<ServiceLine> ServiceLines => Set<ServiceLine>();
    public DbSet<ModifierGroup> ModifierGroups => Set<ModifierGroup>();
    public DbSet<Modifier> Modifiers => Set<Modifier>();
    public DbSet<ProductModifier> ProductModifiers => Set<ProductModifier>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
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
        b.Entity<Branch>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Branch>().Property(x => x.Code).HasMaxLength(10);
        b.Entity<Guest>().HasIndex(x => x.Phone).IsUnique();
        // Rows created before this column existed get the migration time, not year 1.
        b.Entity<Guest>().Property(x => x.CreatedAtUtc).HasDefaultValueSql("now()");
        b.Entity<Ticket>().HasIndex(x => new { x.BranchId, x.Folio }).IsUnique();
        b.Entity<Ticket>().HasIndex(x => x.PublicOrderId).IsUnique();
        b.Entity<Garment>().HasIndex(x => x.Code).IsUnique();
        b.Entity<ProductModifier>().HasIndex(x => new { x.ProductId, x.ModifierId }).IsUnique();
        b.Entity<ServiceLine>().HasIndex(x => x.Name).IsUnique();
        b.Entity<ModifierGroup>().HasIndex(x => new { x.ServiceLineId, x.Name }).IsUnique();
        b.Entity<Modifier>().HasIndex(x => new { x.ModifierGroupId, x.Name }).IsUnique();
        b.Entity<Product>().HasIndex(x => new { x.ServiceLineId, x.Name }).IsUnique();
        b.Entity<Payment>().HasIndex(x => x.ShiftId);
        b.Entity<PriceList>().HasIndex(x => x.Name).IsUnique();
        // One price per product per moment; also the index used to find the current price.
        b.Entity<PriceListItem>().HasIndex(x => new { x.PriceListId, x.ProductId, x.EffectiveFromUtc }).IsUnique();
        b.Entity<Modifier>().Property(x => x.ChargeValue).HasPrecision(12, 4);

        ConfigureForeignKeys(b);

        // Business data is never deleted (see docs/data-rules.md §2), so the database
        // refuses to delete any row that other rows still point to.
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    /// <summary>
    /// Relationships whose entity has only the ID column and no navigation property.
    /// Without this, EF Core would not create a real foreign key for them.
    /// </summary>
    private static void ConfigureForeignKeys(ModelBuilder b)
    {
        b.Entity<Attendance>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        b.Entity<Attendance>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId);

        b.Entity<Shift>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId);
        b.Entity<Shift>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId);

        b.Entity<AuditLog>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        b.Entity<AuditLog>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId);

        b.Entity<Ticket>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId);
        b.Entity<Ticket>().HasOne<User>().WithMany().HasForeignKey(x => x.CashierId);
        b.Entity<Ticket>().HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId);

        b.Entity<PriceListItem>().HasOne<User>().WithMany().HasForeignKey(x => x.SetByUserId);
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
