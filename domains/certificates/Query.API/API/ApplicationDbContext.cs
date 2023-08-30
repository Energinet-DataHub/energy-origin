using API.ContractService;
using Microsoft.EntityFrameworkCore;

namespace API;

//TODO: Should this be in a shared lib?
//TODO: What about location of migration scripts?
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CertificateIssuingContract>()
            .HasIndex(c => new { c.GSRN, c.ContractNumber })
            .IsUnique();
    }

    public DbSet<CertificateIssuingContract> Contracts { get; set; }
}
