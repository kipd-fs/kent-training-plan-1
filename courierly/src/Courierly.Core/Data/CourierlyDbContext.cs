using Courierly.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Courierly.Core.Data;

public class CourierlyDbContext : DbContext
{
    public CourierlyDbContext(DbContextOptions<CourierlyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<FeeRule> FeeRules => Set<FeeRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(100);
            e.Property(t => t.TimeZoneId).HasMaxLength(64);
        });

        modelBuilder.Entity<Merchant>(e =>
        {
            e.Property(m => m.Name).HasMaxLength(100);
            e.Property(m => m.CallbackUrl).HasMaxLength(400);
            e.Property(m => m.ApiKey).HasMaxLength(200);
        });

        modelBuilder.Entity<Shipment>(e =>
        {
            e.Property(s => s.Reference).HasMaxLength(40);
            e.Property(s => s.Zone).HasMaxLength(10);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.WeightKg).HasPrecision(9, 3);
            e.Property(s => s.Fee).HasPrecision(12, 2);
            e.HasIndex(s => new { s.TenantId, s.Reference }).IsUnique();
        });

        modelBuilder.Entity<FeeRule>(e =>
        {
            e.Property(r => r.Zone).HasMaxLength(10);
            e.Property(r => r.PromoCode).HasMaxLength(40);
            e.Property(r => r.BaseFee).HasPrecision(12, 2);
            e.Property(r => r.PerKgFee).HasPrecision(12, 2);
            e.Property(r => r.MinimumFee).HasPrecision(12, 2);
            e.Property(r => r.WeekendSurchargePct).HasPrecision(5, 2);
            e.Property(r => r.AfterHoursSurchargePct).HasPrecision(5, 2);
            e.Property(r => r.PromoDiscountPct).HasPrecision(5, 2);
            e.HasIndex(r => new { r.TenantId, r.Zone }).IsUnique();
        });
    }
}
