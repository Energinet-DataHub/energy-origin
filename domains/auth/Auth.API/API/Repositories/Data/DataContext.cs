using API.Models.Entities;
using API.Repositories.Data.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.Repositories.Data;

public class DataContext : DbContext, IUserDataContext, ICompanyDataContext, IUserProviderDataContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<UserProvider> UserProviders { get; set; } = null!;

    public DataContext(DbContextOptions options, NpgsqlDataSourceBuilder dataSourceBuilder) : base(options) => dataSourceBuilder.MapEnum<ProviderKeyType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<ProviderKeyType>();
    }
}
