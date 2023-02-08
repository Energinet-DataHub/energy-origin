using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Data
{
    public class DataContext : DbContext, IUserDataContext
    {
        protected readonly IConfiguration Configuration;

        public DbSet<User> Users { get; set; }

#pragma warning disable CS8618
        public DataContext(DbContextOptions options, IConfiguration configuration) : base(options) => Configuration = configuration;
#pragma warning restore CS8618
        protected override void OnModelCreating(ModelBuilder modelBuilder) => base.OnModelCreating(modelBuilder);
    }
}
