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

        modelBuilder.Entity<Client>()
            .Property(e => e.IdpClientId)
            .HasConversion(new ValueConverter<IdpClientId, Guid>(
                v => v.Value,
                v => new IdpClientId(v)))
            .HasColumnName("IdpClientId");

        modelBuilder.Entity<User>()
            .Property(u => u.IdpId)
            .HasConversion(new ValueConverter<IdpId, Guid>(
                v => v.Value,
                v => new IdpId(v)))
            .HasColumnName("IdpId");

        modelBuilder.Entity<Organization>()
            .Property(o => o.IdpId)
            .HasConversion(new ValueConverter<IdpId, Guid>(
                v => v.Value,
                v => new IdpId(v)))
            .HasColumnName("IdpId");

        modelBuilder.Entity<Client>()
            .Property(c => c.Name)
            .HasConversion(new ValueConverter<Name, string>(
                v => v.Value,
                v => new Name(v)))
            .HasColumnName("Name");

        modelBuilder.Entity<User>()
            .Property(u => u.Name)
            .HasConversion(new ValueConverter<Name, string>(
                v => v.Value,
                v => new Name(v)))
            .HasColumnName("Name");

        modelBuilder.Entity<User>()
            .Property(u => u.IdpUserId)
            .HasConversion(new ValueConverter<IdpUserId, Guid>(
                v => v.Value,
                v => new IdpUserId(v)))
            .HasColumnName("IdpUserId");

        modelBuilder.Entity<Organization>()
            .Property(o => o.IdpOrganizationId)
            .HasConversion(new ValueConverter<IdpOrganizationId, Guid>(
                v => v.Value,
                v => new IdpOrganizationId(v)))
            .HasColumnName("IdpOrganizationId");

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
            .WithMany(cl => cl.Consents)
            .HasForeignKey(c => c.ClientId);

        modelBuilder.Entity<Affiliation>()
            .HasIndex(a => new { a.UserId, a.OrganizationId })
            .IsUnique();

        modelBuilder.Entity<Consent>()
            .HasIndex(c => new { c.ClientId, c.OrganizationId })
            .IsUnique();

        modelBuilder.Entity<User>().HasIndex(u => u.IdpUserId).IsUnique();
        modelBuilder.Entity<Organization>().HasIndex(o => o.IdpOrganizationId).IsUnique();
        modelBuilder.Entity<Client>().HasIndex(c => c.IdpClientId).IsUnique();
    }
}
