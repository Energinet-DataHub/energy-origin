using API.IntegrationTests.Setup;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace API.IntegrationTests.Migrations;

public class MigrationTests : IntegrationTestBase, IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public MigrationTests(IntegrationTestFixture fixture) : base(fixture)
    {
        var newDatabaseInfo = _fixture.PostgresContainer.CreateNewDatabase().Result;
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(newDatabaseInfo.ConnectionString).Options;
        using var dbContext = new ApplicationDbContext(_options);
        dbContext.Database.Migrate();
    }

    [Fact]
    public async Task can_apply_all_migrations()
    {
        await using var dbContext = new ApplicationDbContext(_options);
        var migrator = dbContext.Database.GetService<IMigrator>();

        var applyAllMigrations = async () => await migrator.MigrateAsync();

        await applyAllMigrations.Should().NotThrowAsync();
    }
}
