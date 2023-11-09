using API.ContractService;
using API.DataSyncSyncer.Persistence;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CertificateIssuingContract>().HasIndex(c => new { c.GSRN, c.ContractNumber }).IsUnique();
        modelBuilder.Entity<CertificateIssuingContract>(entity =>
        {
            entity.OwnsOne(e => e.Technology, tech =>
            {
                tech.Property(t => t.FuelCode).HasDefaultValue("F00000000");
                tech.Property(t => t.TechCode).HasDefaultValue("T070000");
            });
        });

        modelBuilder.Entity<ProductionCertificate>().OwnsOne(c => c.Technology);
        modelBuilder.Entity<ProductionCertificate>().HasIndex(c => new { c.Gsrn, c.DateFrom, c.DateTo }).IsUnique();

        modelBuilder.Entity<SynchronizationPosition>().HasKey(s => s.GSRN);

        modelBuilder.Entity<ConsumptionCertificate>().HasIndex(c => new { c.Gsrn, c.DateFrom, c.DateTo }).IsUnique();
    }

    public DbSet<CertificateIssuingContract> Contracts { get; set; }
    public DbSet<ProductionCertificate> ProductionCertificates { get; set; }
    public DbSet<SynchronizationPosition> SynchronizationPositions { get; set; }
    public DbSet<ConsumptionCertificate> ConsumptionCertificates { get; set; }
}
