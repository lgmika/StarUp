using Microsoft.EntityFrameworkCore;
using StartupConnect.Domain.Entities;

namespace StartupConnect.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(auditLog => auditLog.Id);

            entity.Property(auditLog => auditLog.Action).HasMaxLength(120).IsRequired();
            entity.Property(auditLog => auditLog.ResourceType).HasMaxLength(120).IsRequired();
            entity.Property(auditLog => auditLog.Reason).HasMaxLength(500);
            entity.Property(auditLog => auditLog.IpAddress).HasMaxLength(64);
            entity.Property(auditLog => auditLog.UserAgent).HasMaxLength(500);
        });
    }
}

