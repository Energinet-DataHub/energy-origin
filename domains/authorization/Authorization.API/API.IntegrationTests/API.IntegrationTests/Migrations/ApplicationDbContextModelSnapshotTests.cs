using System.Data;
using API.IntegrationTests.Setup;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace API.IntegrationTests.Migrations;

public class ApplicationDbContextModelSnapshotTests : IntegrationTestBase, IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public ApplicationDbContextModelSnapshotTests(IntegrationTestFixture fixture) : base(fixture)
    {
        var newDatabaseInfo = _fixture.PostgresContainer.CreateNewDatabase().Result;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
        using var dbContext = new ApplicationDbContext(_options);
        dbContext.Database.Migrate();
    }

    [Fact]
    public async Task model_snapshot_matches_database_schema()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        await dbContext.Database.MigrateAsync();

        var modelSnapshot = dbContext.Model;

        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        var databaseSchema = await connection.GetSchemaAsync("Tables");

        var snapshotTables = modelSnapshot.GetEntityTypes()
            .Select(et => et.GetTableName())
            .Distinct();

        var schemaTables = databaseSchema.Rows
            .Cast<DataRow>()
            .Select(row => row["TABLE_NAME"].ToString())
            .Where(tableName => tableName != "__EFMigrationsHistory")
            .Distinct();

        snapshotTables.Should().BeEquivalentTo(schemaTables);
    }
}
