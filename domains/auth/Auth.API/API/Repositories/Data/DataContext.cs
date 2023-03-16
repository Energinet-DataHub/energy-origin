using API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Data;

public class DataContext : DbContext, IUserDataContext, ICompanyDataContext
{
    protected readonly IConfiguration Configuration;

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;

    public DataContext(DbContextOptions options, IConfiguration configuration) : base(options) => Configuration = configuration;

    protected override void OnModelCreating(ModelBuilder modelBuilder) => base.OnModelCreating(modelBuilder);
}
