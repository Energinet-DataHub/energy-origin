using DataContext.Models;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Domain.ValueObjects.Converters;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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
            .HaveConversion<UnixTimestampValueToSecondsConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CertificateIssuingContract>().HasIndex(c => new { c.GSRN, c.ContractNumber }).IsUnique();
        modelBuilder.Entity<CertificateIssuingContract>().OwnsOne(c => c.Technology);
        modelBuilder.Entity<CertificateIssuingContract>().Property(c => c.SponsorshipEndDate).HasColumnType("timestamp with time zone");

        modelBuilder.Entity<MeteringPointTimeSeriesSlidingWindow>().HasKey(s => new { s.GSRN });
        modelBuilder.Entity<MeteringPointTimeSeriesSlidingWindow>().OwnsOne(m => m.MissingMeasurements, d =>
        {
            d.ToJson();
            d.OwnsMany(x => x.Intervals);
        });

        modelBuilder.Entity<Sponsorship>(b =>
        {
            b.HasKey(s => s.SponsorshipGSRN);

            b.Property(s => s.SponsorshipGSRN)
                .HasColumnName("SponsorshipGSRN")
                .HasConversion(
                    gsrnValueObject => gsrnValueObject.Value,
                    stringValueWhenFetchedFromDatabase => new Gsrn(stringValueWhenFetchedFromDatabase)
                );

            b.Property(s => s.SponsorshipEndDate)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.AddActivityLogEntry();
    }

    public DbSet<CertificateIssuingContract> Contracts { get; set; }
    public DbSet<ActivityLogEntry> ActivityLogs { get; set; }
    public DbSet<MeteringPointTimeSeriesSlidingWindow> MeteringPointTimeSeriesSlidingWindows { get; set; }
    public DbSet<Sponsorship> Sponsorships { get; set; }
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


