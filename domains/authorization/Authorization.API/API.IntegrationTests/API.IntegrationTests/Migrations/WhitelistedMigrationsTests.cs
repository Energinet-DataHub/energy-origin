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

        await using (var dbContext = new ApplicationDbContext(options))
        {
            var orgName1 = Any.OrganizationName().Value;
            var orgName2 = Any.OrganizationName().Value;
            var orgName3 = Any.OrganizationName().Value;
            var tin2 = Any.Tin().Value;
            var tin3 = Any.Tin().Value;
            var sql = $@"
            INSERT INTO ""Organizations"" (""Id"", ""Name"", ""Tin"", ""TermsAccepted"", ""ServiceProviderTermsAccepted"")
            VALUES
                ('{Guid.NewGuid()}', '{orgName1.Replace("'", "''")}', NULL, false, false),
                ('{Guid.NewGuid()}', '{orgName2.Replace("'", "''")}', '{tin2.Replace("'", "''")}', false, false),
                ('{Guid.NewGuid()}', '{orgName3.Replace("'", "''")}', '{tin3.Replace("'", "''")}', false, false);
        ";
            await dbContext.Database.ExecuteSqlRawAsync(sql, CancellationToken.None);
        }

        await dbMigrator.MigrateAsync("20250320-0011-CopyOrganizationRecordsToWhitelistedTable.sql");

        await using (var dbContext = new ApplicationDbContext(options))
        {
            var whitelistedRecords = await dbContext.Whitelisted.ToListAsync(TestContext.Current.CancellationToken);

            Assert.DoesNotContain(whitelistedRecords, w => w.Tin.Value == null);
        }
    }

    private async Task<DatabaseInfo> StartPostgresDatabase()
    {
        await _postgresContainer.InitializeAsync();
        return await _postgresContainer.CreateNewDatabase();
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _postgresContainer.DisposeAsync();
}
