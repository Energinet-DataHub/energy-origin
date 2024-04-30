using API.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace API.IntegrationTests.Migrations
{
    [Collection(IntegrationTestCollection.CollectionName)]
    public class RollbackMigrationTests
    {
        private readonly DbContextOptions<ApplicationDbContext> options;

        public RollbackMigrationTests(MigrationTestFixture migrationTestFixture)
        {
            var newDatabaseInfo = migrationTestFixture.PostgresContainer.CreateNewDatabase().Result;
            options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(newDatabaseInfo.ConnectionString)
                .Options;

            using var dbContext = new ApplicationDbContext(options);
            dbContext.Database.Migrate();
        }

        [Fact]
        public async Task can_rollback_all_migrations()
        {
            await using var dbContext = new ApplicationDbContext(options);

            var migrator = dbContext.Database.GetService<IMigrator>();

            var rollbackAllMigrations = async () => await migrator.MigrateAsync("0");

            await rollbackAllMigrations.Should().NotThrowAsync();
        }

        [Fact]
        public async Task can_apply_all_migrations()
        {
            await using var dbContext = new ApplicationDbContext(options);

            var migrator = dbContext.Database.GetService<IMigrator>();

            var applyAllMigrations = async () => await migrator.MigrateAsync();

            await applyAllMigrations.Should().NotThrowAsync();

            var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
            var allMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

            appliedMigrations.Should().BeEquivalentTo(allMigrations, assertionOptions =>
                assertionOptions.WithStrictOrdering());
        }
    }
}
