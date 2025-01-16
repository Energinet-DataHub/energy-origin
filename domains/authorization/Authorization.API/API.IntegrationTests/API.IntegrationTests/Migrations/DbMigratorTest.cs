using API.IntegrationTests.Setup;
using API.Migrations;
using API.Models;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.IntegrationTests.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class DbMigratorTest
{
    private readonly DatabaseInfo _databaseInfo;
    private readonly DbMigrator _dbMigrator;

    public DbMigratorTest(IntegrationTestFixture integrationTestFixture)
    {
        _databaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        _dbMigrator = new DbMigrator(_databaseInfo.ConnectionString, NullLogger<DbMigrator>.Instance);
    }

    [Fact]
    public async Task GivenNewDatabase_WhenApplyingMigrations_AllMigrationsAreApplied()
    {
        await _dbMigrator.MigrateAsync();

        var scripts = await GetAppliedMigrations();
        Assert.True(scripts.Count > 0);
        Assert.Equal("20250114-0001-InitialSchema.sql", scripts.First());
    }

    [Fact]
    public async Task GivenNewDatabase_WhenApplyingMigrationsWithTarget_EarlierMigrationsAreApplied()
    {
        await _dbMigrator.MigrateAsync("20250115");

        var scripts = await GetAppliedMigrations();
        Assert.True(scripts.Count == 1);
        Assert.Equal("20250114-0001-InitialSchema.sql", scripts.First());
    }

    [Fact]
    public async Task GivenNewDatabase_WhenApplyingMigrationsWithTarget_LaterMigrationsAreNotApplied()
    {
        await _dbMigrator.MigrateAsync("20250113");

        var scripts = await GetAppliedMigrations();
        Assert.Empty(scripts);
    }

    private async Task<List<string>> GetAppliedMigrations()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_databaseInfo.ConnectionString)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        var migrationsTableExists = (await dbContext.Database
                .SqlQueryRaw<bool>("SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'schemaversions')")
                .ToListAsync())
            .First();

        if (!migrationsTableExists)
        {
            return new List<string>();
        }

        return await dbContext.Database.SqlQueryRaw<string>("SELECT scriptname FROM public.schemaversions ORDER BY Applied ASC").ToListAsync();
    }
}
