using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Data
{
    public class DataContext : DbContext, IUserDataContext
    {
        protected readonly IConfiguration Configuration;

        public DbSet<User> Users { get; set; } = null!;

        public DataContext(DbContextOptions options, IConfiguration configuration) : base(options) => Configuration = configuration;

        protected override void OnModelCreating(ModelBuilder modelBuilder) => base.OnModelCreating(modelBuilder);
    }
}
