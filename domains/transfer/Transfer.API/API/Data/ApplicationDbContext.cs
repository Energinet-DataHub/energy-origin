using API.Models;
using Audit.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class ApplicationDbContext : AuditDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<TransferAgreement> TransferAgreements { get; set; } = null!;
    public DbSet<TransferAgreementHistoryEntry> TransferAgreementHistoryEntries { get; set; } = null!;
    public DbSet<ConnectionInvitation> ConnectionInvitations { get; set; } = null!;
    public DbSet<Connection> Connections { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConnectionInvitation>()
            .Property(b => b.CreatedAt)
            .HasDefaultValueSql("current_timestamp at time zone 'UTC'");

        modelBuilder.Entity<TransferAgreement>()
            .HasIndex(nameof(TransferAgreement.SenderId), nameof(TransferAgreement.TransferAgreementNumber))
            .IsUnique();
    }
}
