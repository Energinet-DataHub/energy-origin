using System;
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

        ConfigureOrganizationTable(modelBuilder);
        ConfigureAffiliationTable(modelBuilder);
        ConfigureConsentTable(modelBuilder);
        ConfigureClientTable(modelBuilder);
        ConfigureUserTable(modelBuilder);
    }

    private static void ConfigureOrganizationTable(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>().Property(o => o.Name)
            .HasConversion(new ValueConverter<OrganizationName, string>(v => v.Value, v => new OrganizationName(v)))
            .IsRequired();

        modelBuilder.Entity<Organization>().Property(o => o.Tin)
            .HasConversion(new ValueConverter<Tin, string>(v => v.Value, v => new Tin(v)))
            .IsRequired();

        modelBuilder.Entity<Organization>().HasMany(it => it.Affiliations);
    }

    private static void ConfigureClientTable(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>().Property(e => e.IdpClientId)
            .HasConversion(new ValueConverter<IdpClientId, Guid>(v => v.Value, v => new IdpClientId(v)))
            .IsRequired();

        modelBuilder.Entity<Client>().Property(c => c.Name)
            .HasConversion(new ValueConverter<ClientName, string>(v => v.Value, v => ClientName.Create(v)))
            .IsRequired();

        modelBuilder.Entity<Client>().HasIndex(c => c.IdpClientId).IsUnique();
    }

    private static void ConfigureUserTable(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<User>().Property(u => u.Username)
            .HasConversion(new ValueConverter<Username, string>(v => v.Value, v => Username.Create(v)))
            .IsRequired();

        modelBuilder.Entity<User>().Property(u => u.IdpUserId)
            .HasConversion(new ValueConverter<IdpUserId, Guid>(v => v.Value, v => IdpUserId.Create(v)))
            .IsRequired();

        modelBuilder.Entity<User>().HasMany(it => it.Affiliations);

        modelBuilder.Entity<User>().HasIndex(u => u.IdpUserId).IsUnique();
    }

    private static void ConfigureAffiliationTable(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Affiliation>()
            .HasKey(a => new { a.UserId, a.OrganizationId });
    }

    private static void ConfigureConsentTable(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Consent>()
            .HasIndex(c => new { c.ClientId, c.OrganizationId })
            .IsUnique();
    }
}
