using System;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Setup.Migrations;
using EnergyTrackAndTrace.Testing.Testcontainers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace API.IntegrationTests.ClaimAutomation.Api.Repository;

public class ClaimAutomationRepositoryTest(PostgresDatabase postgresDatabase) : IClassFixture<PostgresDatabase>
{
    [Fact]
    public async Task exception_is_thrown_when_duplicated_claim_automation_argument()
    {
        await using var dbContext = await CreateNewCleanDatabase();

        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);

        dbContext.ClaimAutomationArguments.Add(claimAutomationArgument);
        await dbContext.SaveChangesAsync();

        dbContext.ClaimAutomationArguments.Add(claimAutomationArgument);
        dbContext.Invoking(db => db.SaveChanges()).Should().Throw<DbUpdateException>();
    }

    private async Task<ApplicationDbContext> CreateNewCleanDatabase()
    {
        await postgresDatabase.InitializeAsync();
        var database = await postgresDatabase.CreateNewDatabase();

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(database.ConnectionString)
            .Options;

        new DbMigrator(database.ConnectionString, typeof(ApplicationDbContext).Assembly, NullLogger<DbMigrator>.Instance).MigrateAsync().Wait();

        var dbContext = new ApplicationDbContext(contextOptions);
        return dbContext;
    }
}
