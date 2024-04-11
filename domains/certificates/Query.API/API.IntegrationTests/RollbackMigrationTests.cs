using API.IntegrationTests.Factories;
using DataContext;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Testing.Testcontainers;
using Xunit;

namespace API.IntegrationTests;

public class RollbackMigrationTests :
    TestBase,
    IClassFixture<QueryApiWebApplicationFactory>,
    IClassFixture<PostgresContainer>
{
    private readonly QueryApiWebApplicationFactory factory;

    public RollbackMigrationTests(QueryApiWebApplicationFactory factory, PostgresContainer dbContainer)
    {
        this.factory = factory;
        this.factory.ConnectionString = dbContainer.ConnectionString;
    }

    [Fact]
    public async Task can_rollback_all_migrations()
    {
        factory.Start();
        var dbContextFactory = factory.Services.GetRequiredService<IDbContextFactory<CertificateDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var migrator = dbContext.Database.GetService<IMigrator>();

        var rollbackAllMigrations = () => migrator.Migrate("0");
        rollbackAllMigrations.Should().NotThrow();
    }
}
