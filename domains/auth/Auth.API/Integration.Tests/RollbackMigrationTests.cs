using Integration.Tests;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests;

public class RollbackMigrationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;

    public RollbackMigrationTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public void Migrate_ShouldRollbackMigrationsAndThenApplyMigrations_WhenInvokedWithZeroAndThenNoArguments()
    {
        var migrator = factory.DataContext.Database.GetService<IMigrator>(); //Factory migrates the database to latest migration automatically
        migrator.Migrate("0"); //This migrates down to migration no. 0 aka. remove all migrations
        migrator.Migrate(); //This migrates the database to latest migration (again)
    }
}
