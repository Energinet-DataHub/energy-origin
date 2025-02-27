using DataContext;
using EnergyOrigin.Setup.Migrations;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.IntegrationTests.Mocks;

public class DbContextFactoryMock : IDbContextFactory<ApplicationDbContext>
{
    private readonly DatabaseInfo _databaseInfo;

    public DbContextFactoryMock(PostgresContainer dbContainer)
    {
        _databaseInfo = dbContainer.CreateNewDatabase().GetAwaiter().GetResult();
        new DbMigrator(_databaseInfo.ConnectionString, typeof(ApplicationDbContext).Assembly, NullLogger<DbMigrator>.Instance).MigrateAsync().Wait();
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_databaseInfo.ConnectionString).Options;
        return new ApplicationDbContext(options);
    }
}
