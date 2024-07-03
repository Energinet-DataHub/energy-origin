using API.MeteringPoints.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace API.MeteringPoints.Api;

public class ApplicationDbContext : DbContext
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }

    public DbSet<RelationDto> Relations { get; set; }
    public DbSet<OrgAcceptedTermsEvent> OrgAcceptedTermsEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RelationDto>().HasKey(r => r.SubjectId);
    }
}
