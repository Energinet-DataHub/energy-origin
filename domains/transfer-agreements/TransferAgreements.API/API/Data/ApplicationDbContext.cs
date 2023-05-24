using API.ApiModels;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<TransferAgreement> TransferAgreements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
}
