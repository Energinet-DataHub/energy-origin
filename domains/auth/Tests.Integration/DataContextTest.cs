using API.Models;
using API.Repositories.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Tests.Integration
{
    public class DataContextTest : DbContext, IUserDataContext
    {
        protected readonly IConfiguration Configuration;

        public DbSet<User> Users { get; set; }

        public DataContextTest(DbContextOptions options, IConfiguration configuration) : base(options)
        {
            Configuration = configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => base.OnConfiguring(optionsBuilder);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
