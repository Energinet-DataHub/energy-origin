using API.Claiming.Api.Models;
using API.Transfer.Api.Models;
using Audit.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace API.Shared.Data;

public class ApplicationDbContext : AuditDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<TransferAgreement> TransferAgreements { get; set; } = null!;
    public DbSet<TransferAgreementHistoryEntry> TransferAgreementHistoryEntries { get; set; } = null!;
    public DbSet<ClaimAutomationArgument> ClaimAutomationArguments { get; set; } = null!;
    public DbSet<TransferAgreementProposal> TransferAgreementProposals { get; set; } = null!;

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
    }
}
