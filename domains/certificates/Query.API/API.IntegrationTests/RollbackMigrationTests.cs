using System.Threading.Tasks;
using API.IntegrationTests.Factories;
using DataContext;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public class RollbackMigrationTests : TestBase
{
    private readonly QueryApiWebApplicationFactory factory;

    public RollbackMigrationTests(IntegrationTestFixture integrationTestFixture)
    {
        factory = integrationTestFixture.WebApplicationFactory;
    }

    [Fact]
    public async Task can_rollback_all_migrations()
    {
        factory.Start();
        var dbContextFactory = factory.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var migrator = dbContext.Database.GetService<IMigrator>();

        var rollbackAllMigrations = () => migrator.Migrate("0");
        rollbackAllMigrations.Should().NotThrow();
    }
}
