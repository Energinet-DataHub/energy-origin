using API.Repositories.Data;
using Integration.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests;

public class RollbackMigrationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory factory;

    public RollbackMigrationTests(AuthWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task can_rollback_all_migrations()
    {
        factory.Start(); //This migrates the database to latest migration
        var dbContextFactory = factory.Services.GetRequiredService<IDbContextFactory<DataContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var migrator = dbContext.Database.GetService<IMigrator>();

        migrator.Migrate("0"); //This migrates down to migration no. 0 aka. remove all migrations
    }
}
