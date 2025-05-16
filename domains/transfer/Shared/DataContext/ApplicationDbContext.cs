using DataContext.Models;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Domain.ValueObjects.Converters;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace DataContext;

public class ApplicationDbContext : DbContext
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }

    public DbSet<ActivityLogEntry> ActivityLogs { get; set; }
    public DbSet<ClaimAutomationArgument> ClaimAutomationArguments { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<TransferAgreement> TransferAgreements { get; set; }
    public DbSet<TransferAgreementProposal> TransferAgreementProposals { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureClaimAutomationArgument(modelBuilder);
        ConfigureReport(modelBuilder);
        ConfigureTransferAgreement(modelBuilder);
        ConfigureTransferAgreementProposal(modelBuilder);

        // EnergyOrigin.ActivityLog
        modelBuilder.AddActivityLogEntry();

        // MassTransit
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    private static void ConfigureClaimAutomationArgument(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClaimAutomationArgument>().HasKey(p => p.SubjectId);
    }

    private static void ConfigureReport(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Report>().Property(r => r.Status).HasConversion<string>().HasColumnType("text").IsRequired();
        modelBuilder.Entity<Report>().Property(r => r.Content).HasColumnType("bytea");
        modelBuilder.Entity<Report>().Property(r => r.CreatedAt).HasConversion(new UnixTimestampValueToDateTimeOffsetConverter());
        modelBuilder.Entity<Report>().Property(o => o.StartDate).HasConversion(new UnixTimestampValueToDateTimeOffsetConverter());
        modelBuilder.Entity<Report>().Property(o => o.EndDate).HasConversion(new UnixTimestampValueToDateTimeOffsetConverter());
    }

    private static void ConfigureTransferAgreement(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransferAgreement>().HasIndex(nameof(TransferAgreement.SenderId), nameof(TransferAgreement.TransferAgreementNumber)).IsUnique();
        modelBuilder.Entity<TransferAgreement>().Property(o => o.SenderTin).HasConversion(new TinValueConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.ReceiverTin).HasConversion(new TinValueConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.StartDate).HasConversion(new UnixTimestampValueToDateTimeOffsetConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.EndDate).HasConversion(new NullableUnixTimestampValueToDateTimeOffsetConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.SenderName).HasConversion(new OrganizationNameValueConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.ReceiverName).HasConversion(new OrganizationNameValueConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.SenderId).HasConversion(new OrganizationIdValueConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.ReceiverId).HasConversion(new NullableOrganizationIdValueConverter());
    }

    private static void ConfigureTransferAgreementProposal(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransferAgreementProposal>().Property(b => b.CreatedAt).HasDefaultValueSql("current_timestamp at time zone 'UTC'");
        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.SenderCompanyTin).HasConversion(new TinValueConverter());
        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.ReceiverCompanyTin).HasConversion(new NullableTinValueConverter());
        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.CreatedAt).HasConversion(new UnixTimestampValueToDateTimeOffsetConverter());
        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.StartDate).HasConversion(new UnixTimestampValueToDateTimeOffsetConverter());
        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.EndDate).HasConversion(new NullableUnixTimestampValueToDateTimeOffsetConverter());
        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.SenderCompanyName).HasConversion(new OrganizationNameValueConverter());
        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.SenderCompanyId).HasConversion(new OrganizationIdValueConverter());
    }
}
