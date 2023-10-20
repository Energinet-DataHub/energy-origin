using API.Connection.Api.Models;
using API.Transfer.Api.Models;
using Audit.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace API.Shared.Data;

public class ApplicationDbContext : AuditDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<TransferAgreement> TransferAgreements { get; set; }
    public DbSet<TransferAgreementHistoryEntry> TransferAgreementHistoryEntries { get; set; }
    public DbSet<ConnectionInvitation> ConnectionInvitations { get; set; }
    public DbSet<Connection.Api.Models.Connection> Connections { get; set; }

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
