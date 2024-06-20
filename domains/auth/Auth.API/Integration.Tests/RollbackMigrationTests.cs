using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Integration.Tests;

public class RollbackMigrationTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task Migrate_ShouldRollbackMigrationsAndThenApplyMigrations_WhenInvokedWithZeroAndThenNoArguments()
    {
        await factory.DataContext.Database.GetService<IMigrator>().MigrateAsync("0");
        await factory.DataContext.Database.GetService<IMigrator>().MigrateAsync();
    }
}
