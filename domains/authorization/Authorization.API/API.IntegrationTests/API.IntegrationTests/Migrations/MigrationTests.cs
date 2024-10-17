using API.IntegrationTests.Setup;
using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace API.IntegrationTests.Migrations;

[Collection(IntegrationTestCollection.CollectionName)]
public class MigrationTests
{
    private readonly DbContextOptions<ApplicationDbContext> options;

    public MigrationTests(IntegrationTestFixture integrationTestFixture)
    {
        var newDatabaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
        options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(newDatabaseInfo.ConnectionString)
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        dbContext.Database.Migrate();
    }

    [Fact]
    public async Task can_apply_all_migrations()
    {
        await using var dbContext = new ApplicationDbContext(options);
        var migrator = dbContext.Database.GetService<IMigrator>();

        var applyAllMigrations = async () => await migrator.MigrateAsync();

        await applyAllMigrations.Should().NotThrowAsync();
    }
}
