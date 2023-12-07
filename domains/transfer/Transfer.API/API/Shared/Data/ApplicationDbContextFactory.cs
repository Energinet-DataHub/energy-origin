using Microsoft.EntityFrameworkCore;

namespace API.Shared.Data;

public class ApplicationDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly string _connectionString;

    public ApplicationDbContextFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public ApplicationDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(_connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
