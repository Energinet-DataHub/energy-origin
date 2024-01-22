using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Integration.Tests;

public class RollbackMigrationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;

    public RollbackMigrationTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async void Migrate_ShouldRollbackMigrationsAndThenApplyMigrations_WhenInvokedWithZeroAndThenNoArguments()
    {
        await factory.DataContext.Database.GetService<IMigrator>().MigrateAsync("0");
        await factory.DataContext.Database.GetService<IMigrator>().MigrateAsync();
    }
}
