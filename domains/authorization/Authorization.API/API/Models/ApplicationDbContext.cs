using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace API.Models;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Affiliation> Affiliations { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Consent> Consents { get; set; }
    public DbSet<AuditRecord> AuditRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Organization>()
            .HasMany(o => o.Affiliations)
            .WithOne(a => a.Organization)
            .HasForeignKey(a => a.OrganizationId);
        modelBuilder.Entity<Organization>()
            .HasMany(o => o.Consents)
            .WithOne(c => c.Organization)
            .HasForeignKey(c => c.OrganizationId);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Affiliations)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId);

        modelBuilder.Entity<Consent>()
            .HasOne(c => c.Client)
            .WithMany()
            .HasForeignKey(c => c.ClientId);

        modelBuilder.Entity<User>().HasIndex(u => u.IdpUserId).IsUnique();
        modelBuilder.Entity<Organization>().HasIndex(o => o.IdpOrganizationId).IsUnique();
        modelBuilder.Entity<Client>().HasIndex(c => c.IdpClientId).IsUnique();

        modelBuilder.Entity<Consent>().Property(c => c.Status).HasDefaultValue(ConsentStatus.Active);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        OnBeforeSaveChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        OnBeforeSaveChanges();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void OnBeforeSaveChanges()
    {
        var entries = ChangeTracker.Entries().Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted));
        foreach (var entry in entries)
        {
            var entityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "Unknown";

            var changes = JsonSerializer.Serialize(entry.CurrentValues.Properties
                .Where(property => entry.CurrentValues[property] != null)
                .ToDictionary(property => property.Name, property => entry.CurrentValues[property]));

            var auditRecord = new AuditRecord
            {
                EntityType = entry.Entity.GetType().Name,
                Timestamp = DateTime.UtcNow,
                EntityId = entityId,
                Operation = entry.State.ToString(),
                Changes = changes,
                UserId = "Retrieve this from your context/session" // This should be replaced with actual user ID retrieval logic
            };

            AuditRecords.Add(auditRecord);
        }
    }

}
