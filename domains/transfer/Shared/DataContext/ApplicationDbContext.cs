using Audit.EntityFramework;
using DataContext.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataContext;

public class ApplicationDbContext : AuditDbContext
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }

    public DbSet<TransferAgreement> TransferAgreements { get; set; }
    public DbSet<TransferAgreementHistoryEntry> TransferAgreementHistoryEntries { get; set; }
    public DbSet<ClaimAutomationArgument> ClaimAutomationArguments { get; set; }
    public DbSet<TransferAgreementProposal> TransferAgreementProposals { get; set; }

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

