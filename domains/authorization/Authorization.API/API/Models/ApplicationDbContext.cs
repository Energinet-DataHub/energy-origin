using Microsoft.EntityFrameworkCore;

namespace API.Models;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Affiliation> Affiliations { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Consent> Consents { get; set; }

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
    }
}

