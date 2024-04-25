using API.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
            .Property(o => o.OrganizationName)
            .HasConversion(new ValueConverter<OrganizationName, string>(
                v => v.Value,
                v => new OrganizationName(v)))
            .HasColumnName("OrganizationName");

        modelBuilder.Entity<Organization>()
            .Property(o => o.Tin)
            .HasConversion(new ValueConverter<Tin, string>(
                v => v.Value,
                v => new Tin(v)))
            .HasColumnName("Tin");

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
    }
}
