using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PtwSystem.Api.Domain.Entities;

namespace PtwSystem.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Permit> Permits => Set<Permit>();
    public DbSet<GasTest> GasTests => Set<GasTest>();
    public DbSet<Isolation> Isolations => Set<Isolation>();
    public DbSet<PermitHistory> PermitHistory => Set<PermitHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(150).IsRequired();
            e.Property(u => u.Company).HasMaxLength(100);
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Permit>(e =>
        {
            e.HasIndex(p => p.PermitNumber).IsUnique();
            e.HasIndex(p => p.Status);
            e.Property(p => p.PermitNumber).HasMaxLength(20).IsRequired();
            e.Property(p => p.Title).HasMaxLength(150).IsRequired();
            e.Property(p => p.WorkDescription).HasMaxLength(2000).IsRequired();
            e.Property(p => p.Location).HasMaxLength(150).IsRequired();
            e.Property(p => p.EquipmentTag).HasMaxLength(50);
            e.Property(p => p.Hazards).HasMaxLength(1000);
            e.Property(p => p.Precautions).HasMaxLength(1000);
            e.Property(p => p.RequiredPpe).HasMaxLength(500);
            e.Property(p => p.LastComment).HasMaxLength(500);

            // Store enums as readable strings in the database
            e.Property(p => p.Type).HasConversion<string>().HasMaxLength(30);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(p => p.RiskLevel).HasConversion<string>().HasMaxLength(10);

            // Restrict deletes on user links so audit data is never lost
            e.HasOne(p => p.RequestedBy).WithMany().HasForeignKey(p => p.RequestedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.ApprovedBy).WithMany().HasForeignKey(p => p.ApprovedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.IssuedBy).WithMany().HasForeignKey(p => p.IssuedById).OnDelete(DeleteBehavior.Restrict);

            e.Ignore(p => p.RequiresGasTest);
            e.Ignore(p => p.RequiresIsolation);
        });

        modelBuilder.Entity<GasTest>(e =>
        {
            e.HasOne(g => g.Permit).WithMany(p => p.GasTests).HasForeignKey(g => g.PermitId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(g => g.TestedBy).WithMany().HasForeignKey(g => g.TestedById).OnDelete(DeleteBehavior.Restrict);
            e.Property(g => g.Remarks).HasMaxLength(500);
        });

        modelBuilder.Entity<Isolation>(e =>
        {
            e.HasOne(i => i.Permit).WithMany(p => p.Isolations).HasForeignKey(i => i.PermitId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.AppliedBy).WithMany().HasForeignKey(i => i.AppliedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.RemovedBy).WithMany().HasForeignKey(i => i.RemovedById).OnDelete(DeleteBehavior.Restrict);
            e.Property(i => i.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(i => i.IsolationPoint).HasMaxLength(100).IsRequired();
            e.Property(i => i.LockNumber).HasMaxLength(30);
            e.Property(i => i.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<PermitHistory>(e =>
        {
            e.HasOne(h => h.Permit).WithMany(p => p.History).HasForeignKey(h => h.PermitId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(h => h.PerformedBy).WithMany().HasForeignKey(h => h.PerformedById).OnDelete(DeleteBehavior.Restrict);
            e.Property(h => h.Action).HasMaxLength(50).IsRequired();
            e.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(h => h.Comment).HasMaxLength(500);
        });

        // All dates are stored in UTC. SQLite loses the "Kind" flag, so mark values as UTC when reading
        // back - otherwise the JSON has no "Z" suffix and the browser shows the wrong time.
        var utc = new ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
        {
            // EF Core applies a non-nullable converter to DateTime? columns too (nulls are never passed in)
            if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                property.SetValueConverter(utc);
        }
    }
}
