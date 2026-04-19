using Byte.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Byte.Infra.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PayrollBatch> PayrollBatches { get; set; }
    public DbSet<PayrollRecord> PayrollRecords { get; set; }
    public DbSet<PayrollCalculation> PayrollCalculations { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PayrollBatch>(e =>
        {
            e.Property(p => p.FileName).HasMaxLength(260).IsRequired();
            e.Property(p => p.BatchStatus).HasMaxLength(20).IsRequired();
            e.Property(p => p.ApprovedBy).HasMaxLength(256);
        });

        modelBuilder.Entity<PayrollRecord>(e =>
        {
            e.Property(p => p.WorkerId).HasMaxLength(100).IsRequired();
            e.Property(p => p.WorkerName).HasMaxLength(200).IsRequired();
            e.Property(p => p.Site).HasMaxLength(100).IsRequired();
            e.Property(p => p.DayRate).HasColumnType("decimal(18,4)");
            e.Property(p => p.AdvanceDeduction).HasColumnType("decimal(18,4)");
        });

        modelBuilder.Entity<PayrollCalculation>(e =>
        {
            e.Property(p => p.BasePay).HasColumnType("decimal(18,4)");
            e.Property(p => p.SiteAllowance).HasColumnType("decimal(18,4)");
            e.Property(p => p.GrossPay).HasColumnType("decimal(18,4)");
            e.Property(p => p.NetPay).HasColumnType("decimal(18,4)");
            e.Property(p => p.Status).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(p => p.EventType).HasMaxLength(50).IsRequired();
            e.Property(p => p.Actor).HasMaxLength(256).IsRequired();
            e.Property(p => p.Detail).HasMaxLength(500);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
