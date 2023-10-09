using API.Data;
using API.IntegrationTests.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace API.IntegrationTests.Migrations;

public class RollbackMigrationTests : IClassFixture<TransferAgreementsApiWebApplicationFactory>
{
    private readonly TransferAgreementsApiWebApplicationFactory factory;

    public RollbackMigrationTests(TransferAgreementsApiWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task can_rollback_all_migrations()
    {
        var dbContextFactory = factory.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var migrator = dbContext.Database.GetService<IMigrator>();

        var rollbackAllMigrations = () => migrator.Migrate("0");
        rollbackAllMigrations.Should().NotThrow();
    }
}
