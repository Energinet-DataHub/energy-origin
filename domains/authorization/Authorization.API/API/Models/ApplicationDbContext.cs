using System;
using API.ValueObjects;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace API.Models;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Affiliation> Affiliations { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<OrganizationConsent> OrganizationConsents { get; set; }
    public DbSet<Terms> Terms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureOrganizationTable(modelBuilder);
        ConfigureAffiliationTable(modelBuilder);
        ConfigureOrganizationConsentTable(modelBuilder);
        ConfigureClientTable(modelBuilder);
        ConfigureUserTable(modelBuilder);
        ConfigureTermsTable(modelBuilder);

        modelBuilder.AddTransactionalOutboxEntities();
    }

    private static void ConfigureOrganizationTable(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>().Property(o => o.Name)
            .HasConversion(new ValueConverter<OrganizationName, string>(v => v.Value, v => new OrganizationName(v)))
            .IsRequired();

        modelBuilder.Entity<Organization>().Property(o => o.Tin).HasConversion(new ValueConverter<Tin?, string>(v => v != null ? v.Value : "", v => new Tin(v)));

        modelBuilder.Entity<Organization>().HasIndex(o => o.Tin).IsUnique();

        modelBuilder.Entity<Organization>().HasMany(it => it.Affiliations);

        modelBuilder.Entity<Organization>().Property(o => o.TermsAccepted)
            .IsRequired()
            .HasDefaultValue(false);

        modelBuilder.Entity<Organization>().Property(o => o.TermsVersion);

        modelBuilder.Entity<Organization>().Property(o => o.TermsAcceptanceDate);
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

        modelBuilder.Entity<Client>()
            .HasOne(x => x.Organization)
            .WithMany(x => x.Clients)
            .HasForeignKey(x => x.OrganizationId);
    }

    private static void ConfigureUserTable(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(u => u.Name)
            .HasConversion(new ValueConverter<UserName, string>(v => v.Value, v => UserName.Create(v)))
            .IsRequired();

        modelBuilder.Entity<User>().Property(u => u.IdpUserId)
            .HasConversion(new ValueConverter<IdpUserId, Guid>(v => v.Value, v => IdpUserId.Create(v)))
            .IsRequired();

        modelBuilder.Entity<User>().HasMany(it => it.Affiliations);

        modelBuilder.Entity<User>().HasIndex(u => u.IdpUserId).IsUnique();
    }

    private static void ConfigureAffiliationTable(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Affiliation>().HasKey(a => new { a.UserId, a.OrganizationId });
    }

    private static void ConfigureOrganizationConsentTable(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganizationConsent>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<OrganizationConsent>()
            .HasOne(x => x.ConsentGiverOrganization)
            .WithMany(x => x.OrganizationGivenConsents)
            .HasForeignKey(x => x.ConsentGiverOrganizationId);

        modelBuilder.Entity<OrganizationConsent>()
            .HasOne(x => x.ConsentReceiverOrganization)
            .WithMany(x => x.OrganizationReceivedConsents)
            .HasForeignKey(x => x.ConsentReceiverOrganizationId);

        // TODO Add unique constraint that we have only 1.

    }

    private static void ConfigureTermsTable(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Terms>().Property(t => t.Version)
            .IsRequired();
        modelBuilder.Entity<Terms>().HasIndex(t => t.Version)
            .IsUnique();
    }
}
