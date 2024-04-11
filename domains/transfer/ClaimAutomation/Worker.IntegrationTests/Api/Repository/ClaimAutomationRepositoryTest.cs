using System;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testing.Testcontainers;
using Xunit;

namespace Worker.IntegrationTests.Api.Repository;

public class ClaimAutomationRepositoryTest(PostgresContainer container) : IClassFixture<PostgresContainer>
{
    [Fact]
    public async Task exception_is_thrown_when_duplicated_claim_automation_argument()
    {
        await using var dbContext = await CreateNewCleanDatabase();
        await dbContext.Database.MigrateAsync();

        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);

        dbContext.ClaimAutomationArguments.Add(claimAutomationArgument);
        await dbContext.SaveChangesAsync();

        dbContext.ClaimAutomationArguments.Add(claimAutomationArgument);
        dbContext.Invoking(db => db.SaveChanges()).Should().Throw<DbUpdateException>();
    }

    private async Task<TransferDbContext> CreateNewCleanDatabase()
    {
        await container.InitializeAsync();

        var contextOptions = new DbContextOptionsBuilder<TransferDbContext>().UseNpgsql(container.ConnectionString)
            .Options;
        var dbContext = new TransferDbContext(contextOptions);
        return dbContext;
    }
}
