using HyundaiTransys.VisionInspection.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HyundaiTransys.VisionInspection.Infrastructure.Persistence;

public sealed class InspectionDbContext : DbContext
{
    public InspectionDbContext(DbContextOptions<InspectionDbContext> options) : base(options) { }

    public DbSet<InspectionRecord> Inspections => Set<InspectionRecord>();
    public DbSet<JobMapping> JobMappings => Set<JobMapping>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<InspectionRecord>(e =>
        {
            e.ToTable("inspections");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.ModelCode, x.SerialNumber });
            e.Property(x => x.ModelCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.SerialNumber).HasMaxLength(64).IsRequired();
            e.Property(x => x.Result).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.ImagePath).HasMaxLength(512);
            e.Property(x => x.RawMesFrame).HasMaxLength(2048);
            e.Property(x => x.OperatorUserName).HasMaxLength(64);
        });

        b.Entity<JobMapping>(e =>
        {
            e.ToTable("job_mappings");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ModelCode, x.VariantKey }).IsUnique();
            e.Property(x => x.ModelCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.VariantKey).HasMaxLength(64);
            e.Property(x => x.Description).HasMaxLength(256);
        });

        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserName).IsUnique();
            e.Property(x => x.UserName).HasMaxLength(64).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(x => x.PasswordSalt).HasMaxLength(128).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
        });
    }
}
