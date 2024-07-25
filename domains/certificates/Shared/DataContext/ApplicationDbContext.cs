using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.ActivityLog.DataContext;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DataContext;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<UnixTimestamp>()
            .HaveConversion<UnixTimestampConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CertificateIssuingContract>().HasIndex(c => new { c.GSRN, c.ContractNumber }).IsUnique();
        modelBuilder.Entity<CertificateIssuingContract>().OwnsOne(c => c.Technology);

        modelBuilder.Entity<MeteringPointTimeSeriesSlidingWindow>().HasKey(s => new { s.GSRN });
        modelBuilder.Entity<MeteringPointTimeSeriesSlidingWindow>().OwnsOne(m => m.MissingMeasurements, d =>
        {
            d.ToJson();
            d.OwnsMany(x => x.Intervals);
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.AddActivityLogEntry();
    }

    public DbSet<CertificateIssuingContract> Contracts { get; set; }
    public DbSet<ActivityLogEntry> ActivityLogs { get; set; }
    public DbSet<MeteringPointTimeSeriesSlidingWindow> MeteringPointTimeSeriesSlidingWindows { get; set; }

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

public class UnixTimestampConverter() : ValueConverter<UnixTimestamp, long>(v => v.Seconds, v => UnixTimestamp.Create(v));
