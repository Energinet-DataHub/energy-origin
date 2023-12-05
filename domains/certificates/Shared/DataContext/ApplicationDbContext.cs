using DataContext.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataContext;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
        
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CertificateIssuingContract>().HasIndex(c => new { c.GSRN, c.ContractNumber }).IsUnique();
        modelBuilder.Entity<CertificateIssuingContract>().OwnsOne(c => c.Technology);

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

// Some of the EF Core Tools commands (for example, the Migrations commands) require a derived DbContext instance to be created at design time
// By implementing the class below, the EF Core Tools will automatically use that for creating the DbContext
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        const string connectionString = "host=localhost;Port=5432;Database=Database;username=postgres;password=postgres;";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
