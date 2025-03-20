using API.Models;
using API.UnitTests;
using EnergyOrigin.Setup.Migrations;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.IntegrationTests.Migrations;

public class WhitelistedMigrationsTests : IAsyncLifetime
{
    private readonly PostgresContainer _postgresContainer = new();

    [Fact]
    public async Task Given_OrganizationsExist_When_MigrationRuns_Then_WhitelistedTableIsPopulatedWithOrganizationWithTinOnly()
    {
        var databaseInfo = await StartPostgresDatabase();
        var dbMigrator = new DbMigrator(databaseInfo.ConnectionString, typeof(Program).Assembly, NullLogger<DbMigrator>.Instance);
        await dbMigrator.MigrateAsync("20250320-0001-AddWhitelistedTable.sql");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(databaseInfo.ConnectionString)
            .Options;

        var organizations = new[]
        {
            Organization.Create(null, Any.OrganizationName()),
            Organization.Create(Any.Tin(), Any.OrganizationName()),
            Organization.Create(Any.Tin(), Any.OrganizationName())
        };

        await using (var dbContext = new ApplicationDbContext(options))
        {
            await dbContext.Organizations.AddRangeAsync(organizations, TestContext.Current.CancellationToken);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await dbMigrator.MigrateAsync("20250320-0011-CopyOrganizationRecordsToWhitelistedTable.sql");

        await using (var dbContext = new ApplicationDbContext(options))
        {
            var whitelistedRecords = await dbContext.Whitelisted.ToListAsync(TestContext.Current.CancellationToken);
            Assert.All(organizations.Skip(1),
                org => Assert.Contains(whitelistedRecords, w => w.Tin.Value == org.Tin?.Value));
        }
    }

    private async Task<DatabaseInfo> StartPostgresDatabase()
    {
        await _postgresContainer.InitializeAsync();
        var databaseInfo = await _postgresContainer.CreateNewDatabase();
        return databaseInfo;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _postgresContainer.DisposeAsync();
}
