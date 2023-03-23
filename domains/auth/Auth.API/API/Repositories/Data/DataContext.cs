using API.Models.Entities;
using API.Repositories.Data.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.Repositories.Data;

public class DataContext : DbContext, IUserDataContext, ICompanyDataContext, IUserProviderDataContext
{
    private readonly IConfiguration configuration;

    public DbSet<User> Users { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<UserProvider> UserProviders { get; set; }

    public DataContext(DbContextOptions options, NpgsqlDataSourceBuilder dataSourceBuilder, IConfiguration configuration) : base(options)
    {
        this.configuration = configuration;

        dataSourceBuilder.MapEnum<ProviderType>();
        dataSourceBuilder.MapEnum<ProviderKeyType>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<ProviderType>();
        modelBuilder.HasPostgresEnum<ProviderKeyType>();
    }
}
