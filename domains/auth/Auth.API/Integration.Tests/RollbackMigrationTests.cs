using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Integration.Tests;

public class RollbackMigrationTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async void Migrate_ShouldRollbackMigrationsAndThenApplyMigrations_WhenInvokedWithZeroAndThenNoArguments()
    {
        await factory.DataContext.Database.GetService<IMigrator>().MigrateAsync("0");
        await factory.DataContext.Database.GetService<IMigrator>().MigrateAsync();
    }
}
