using DataContext;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.Mocks;

public class DbContextFactoryMock : IDbContextFactory<ApplicationDbContext>
{
    private readonly DatabaseInfo _dbInfo;

    public DbContextFactoryMock(PostgresContainer dbContainer)
    {
        _dbInfo = dbContainer.CreateNewDatabase().Result;
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_dbInfo.ConnectionString).Options;
        var dbContext = new ApplicationDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }
}
