using DataContext.Models;
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

    public DbSet<TransferAgreement> TransferAgreements { get; set; }
    public DbSet<ClaimAutomationArgument> ClaimAutomationArguments { get; set; }
    public DbSet<TransferAgreementProposal> TransferAgreementProposals { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransferAgreement>()
            .HasIndex(nameof(TransferAgreement.SenderId), nameof(TransferAgreement.TransferAgreementNumber))
            .IsUnique();

        modelBuilder.Entity<TransferAgreement>().Property(o => o.SenderTin).HasConversion(new TinValueConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.ReceiverTin).HasConversion(new TinValueConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.StartDate).HasConversion(new UnixTimestampValueToDateTimeOffsetConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.EndDate).HasConversion(new NullableUnixTimestampValueToDateTimeOffsetConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.SenderName).HasConversion(new OrganizationNameValueConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.ReceiverName).HasConversion(new OrganizationNameValueConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.SenderId).HasConversion(new OrganizationIdValueConverter());
        modelBuilder.Entity<TransferAgreement>().Property(o => o.ReceiverId).HasConversion(new NullableOrganizationIdValueConverter());

        modelBuilder.Entity<ClaimAutomationArgument>()
            .HasKey(p => p.SubjectId);

        modelBuilder.Entity<TransferAgreementProposal>()
            .Property(b => b.CreatedAt)
            .HasDefaultValueSql("current_timestamp at time zone 'UTC'");

        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.SenderCompanyTin).HasConversion(new TinValueConverter());
        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.ReceiverCompanyTin).HasConversion(new NullableTinValueConverter());
        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.CreatedAt).HasConversion(new UnixTimestampValueToDateTimeOffsetConverter());
        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.StartDate).HasConversion(new UnixTimestampValueToDateTimeOffsetConverter());
        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.EndDate).HasConversion(new NullableUnixTimestampValueToDateTimeOffsetConverter());
        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.SenderCompanyName).HasConversion(new OrganizationNameValueConverter());
        modelBuilder.Entity<TransferAgreementProposal>().Property(o => o.SenderCompanyId).HasConversion(new OrganizationIdValueConverter());

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
