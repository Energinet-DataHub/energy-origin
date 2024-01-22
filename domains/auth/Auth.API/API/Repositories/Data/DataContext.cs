using API.Models.Entities;
using API.Repositories.Data.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.Repositories.Data;

public class DataContext : DbContext, IUserDataContext, ICompanyDataContext, IUserProviderDataContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<UserProvider> UserProviders { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<UserTerms> UserTerms { get; set; } = null!;
    public DbSet<CompanyTerms> CompanyTerms { get; set; } = null!;

    public DataContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<ProviderKeyType>();
        modelBuilder.HasPostgresEnum<CompanyTermsType>();
        modelBuilder.HasPostgresEnum<UserTermsType>();
    }

    public static NpgsqlDataSource GenerateNpgsqlDataSource(string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<UserTermsType>();
        dataSourceBuilder.MapEnum<CompanyTermsType>();
        dataSourceBuilder.MapEnum<ProviderKeyType>();
        return dataSourceBuilder.Build();
    }
}
