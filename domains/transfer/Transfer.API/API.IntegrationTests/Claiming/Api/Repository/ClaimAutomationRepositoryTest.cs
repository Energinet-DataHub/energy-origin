using System;
using System.Threading.Tasks;
using API.Claiming.Api.Models;
using API.IntegrationTests.Factories;
using API.IntegrationTests.Testcontainers;
using API.Shared.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API.IntegrationTests.Claiming.Api.Repository;

public class ClaimAutomationRepositoryTest : IClassFixture<PostgresContainer>
{
    private readonly PostgresContainer container;

    public ClaimAutomationRepositoryTest(PostgresContainer container)
    {
        this.container = container;
    }

    [Fact]
    public async Task exception_is_thrown_when_duplicated_claim_automation_argument()
    {
        await using var dbContext =  await CreateNewCleanDatabase();
        await dbContext.Database.MigrateAsync();

        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);

        dbContext.ClaimAutomationArguments.Add(claimAutomationArgument);
        await dbContext.SaveChangesAsync();

        dbContext.ClaimAutomationArguments.Add(claimAutomationArgument);
        dbContext.Invoking(db => db.SaveChanges()).Should().Throw<DbUpdateException>();
    }

    private async Task<ApplicationDbContext> CreateNewCleanDatabase()
    {
        await container.InitializeAsync();

        var contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(container.ConnectionString)
            .Options;
        var dbContext = new ApplicationDbContext(contextOptions);
        return dbContext;
    }
}
