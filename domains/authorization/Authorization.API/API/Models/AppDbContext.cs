namespace API.Models;

public class AppDbContext : DbContext
{
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Affiliation> Affiliations { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Consent> Consents { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Your Connection String Here");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Affiliation>()
            .HasKey(a => new { a.UserId, a.OrganizationId });
        modelBuilder.Entity<Affiliation>()
            .HasOne(a => a.User)
            .WithMany(u => u.Affiliations)
            .HasForeignKey(a => a.UserId);
        modelBuilder.Entity<Affiliation>()
            .HasOne(a => a.Organization)
            .WithMany(o => o.Affiliations)
            .HasForeignKey(a => a.OrganizationId);

        modelBuilder.Entity<Consent>()
            .HasOne(c => c.Organization)
            .WithMany(o => o.Consents)
            .HasForeignKey(c => c.OrganizationId);
        modelBuilder.Entity<Consent>()
            .HasOne(c => c.Client)
            .WithMany(cl => cl.Consents)
            .HasForeignKey(c => c.ClientId);
    }
}
