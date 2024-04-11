using Audit.EntityFramework;
using DataContext.Models;
using EnergyOrigin.ActivityLog;
using EnergyOrigin.ActivityLog.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataContext;

public class TransferDbContext : AuditDbContext
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TransferDbContext(DbContextOptions<TransferDbContext> options) : base(options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }

    public DbSet<TransferAgreement> TransferAgreements { get; set; }
    public DbSet<TransferAgreementHistoryEntry> TransferAgreementHistoryEntries { get; set; }
    public DbSet<ClaimAutomationArgument> ClaimAutomationArguments { get; set; }
    public DbSet<TransferAgreementProposal> TransferAgreementProposals { get; set; }
    public DbSet<ActivityLogEntry> ActivityLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransferAgreement>()
            .HasIndex(nameof(TransferAgreement.SenderId), nameof(TransferAgreement.TransferAgreementNumber))
            .IsUnique();

        modelBuilder.Entity<ClaimAutomationArgument>()
            .HasKey(p => p.SubjectId);

        modelBuilder.Entity<TransferAgreementProposal>()
            .Property(b => b.CreatedAt)
            .HasDefaultValueSql("current_timestamp at time zone 'UTC'");

        modelBuilder.AddActivityLogEntry();
    }
}
