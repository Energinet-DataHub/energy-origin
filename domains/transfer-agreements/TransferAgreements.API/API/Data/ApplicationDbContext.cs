using Microsoft.EntityFrameworkCore;
using API.ApiModels;

namespace API.Data{

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<TransferAgreement> TransferAgreements { get; set; }
    public DbSet<Subject> Subjects { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransferAgreement>()
            .HasMan
    }
}
}
