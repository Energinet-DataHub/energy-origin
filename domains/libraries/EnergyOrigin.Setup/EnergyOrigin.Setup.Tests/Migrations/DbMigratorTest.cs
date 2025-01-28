using EnergyOrigin.Setup.Migrations;
using EnergyTrackAndTrace.Testing.Testcontainers;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace EnergyOrigin.Setup.Tests.Migrations;

public class DbMigratorTest : IClassFixture<PostgresContainer>
{
    private readonly DatabaseInfo _databaseInfo;
    private readonly DbMigrator _dbMigrator;

    public DbMigratorTest(PostgresContainer postgresContainer)
    {
        _databaseInfo = postgresContainer.CreateNewDatabase().GetAwaiter().GetResult();
        _dbMigrator = new DbMigrator(_databaseInfo.ConnectionString, typeof(DbMigratorTest).Assembly, NullLogger<DbMigrator>.Instance);
    }

    [Fact]
    public async Task GivenNewDatabase_WhenApplyingMigrations_AllMigrationsAreApplied()
    {
        await MigrateSuppressExceptionsAsync();

        var scripts = await GetAppliedMigrations();
        Assert.Equal(2, scripts.Count);
        Assert.Equal("20250116-0001-NoOp1.sql", scripts[0]);
        Assert.Equal("20250116-0010-NoOp2.sql", scripts[1]);
    }

    [Fact]
    public async Task GivenNewDatabase_WhenApplyingFailingMigration_LaterMigrationsAreNotApplied()
    {
        await MigrateSuppressExceptionsAsync();

        var scripts = await GetAppliedMigrations();
        Assert.Equal(2, scripts.Count);
        Assert.DoesNotContain("20250121-0001-Failing.sql", scripts);
        Assert.DoesNotContain("20250121-0010-NoOp3.sql", scripts);
    }

    [Fact]
    public async Task GivenNewDatabase_WhenApplyingMigrationsWithTarget_EarlierMigrationsAreApplied()
    {
        await _dbMigrator.MigrateAsync("20250116-0001-NoOp1.sql");

        var scripts = await GetAppliedMigrations();
        Assert.Single(scripts);
        Assert.Equal("20250116-0001-NoOp1.sql", scripts[0]);
    }

    [Fact]
    public async Task GivenNewDatabase_WhenApplyingMigrations_ItIsPossibleToMigrateInTwoSteps()
    {
        await _dbMigrator.MigrateAsync("20250116-0001-NoOp1.sql");
        var scripts = await GetAppliedMigrations();
        Assert.Single(scripts);
        Assert.Equal("20250116-0001-NoOp1.sql", scripts[0]);

        await _dbMigrator.MigrateAsync("20250116-0010-NoOp2.sql");
        scripts = await GetAppliedMigrations();
        Assert.Equal(2, scripts.Count);
        Assert.Equal("20250116-0010-NoOp2.sql", scripts[1]);
    }

    [Fact]
    public async Task GivenNewDatabase_WhenApplyingMigrationsWithTarget_LaterMigrationsAreNotApplied()
    {
        await _dbMigrator.MigrateAsync("20250115");

        var scripts = await GetAppliedMigrations();
        Assert.Empty(scripts);
    }

    private async Task<List<string>> GetAppliedMigrations()
    {
        await using var conn = new NpgsqlConnection(_databaseInfo.ConnectionString);
        await conn.OpenAsync();

        await using (var migrationsTableCmd = conn.CreateCommand())
        {
            migrationsTableCmd.CommandText = "SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'schemaversions')";
            await using var migrationsTableReader = await migrationsTableCmd.ExecuteReaderAsync();
            await migrationsTableReader.ReadAsync();
            var migrationsTableExists = migrationsTableReader.GetBoolean(0);

            if (!migrationsTableExists)
            {
                return new List<string>();
            }
        }

        await using (var scriptsCmd = conn.CreateCommand())
        {
            scriptsCmd.CommandText = "SELECT scriptname FROM public.schemaversions ORDER BY Applied ASC";
            await using var scriptsReader = await scriptsCmd.ExecuteReaderAsync();

            var result = new List<string>();
            while (await scriptsReader.ReadAsync())
            {
                result.Add(scriptsReader.GetString(0));
            }

            return result;
        }
    }

    private async Task MigrateSuppressExceptionsAsync()
    {
        try
        {
            await _dbMigrator.MigrateAsync();
        }
        catch (Exception)
        {
            // Ignore
        }
    }
}
