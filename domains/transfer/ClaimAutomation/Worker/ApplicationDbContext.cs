using ClaimAutomation.Worker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimAutomation.Worker;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<ClaimAutomationArgument> ClaimAutomationArguments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClaimAutomationArgument>()
            .HasKey(p => p.SubjectId);
    }
}
