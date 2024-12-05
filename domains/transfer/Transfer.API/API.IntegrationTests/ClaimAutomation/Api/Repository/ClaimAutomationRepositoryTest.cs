using System;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using EnergyTrackAndTrace.Testing.Testcontainers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API.IntegrationTests.ClaimAutomation.Api.Repository;

[Collection(IntegrationTestCollection.CollectionName)]
public class ClaimAutomationRepositoryTest(IntegrationTestFixture integrationTestFixture)
{
    [Fact]
    public async Task exception_is_thrown_when_duplicated_claim_automation_argument()
    {
        var emptyDb = await integrationTestFixture.PostgresContainer.CreateNewDatabase();
        var _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(emptyDb.ConnectionString).Options;
        using var dbContext = new ApplicationDbContext(_options);
        dbContext.Database.EnsureCreated();

        var claimAutomationArgument = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.UtcNow);

        dbContext.ClaimAutomationArguments.Add(claimAutomationArgument);
        await dbContext.SaveChangesAsync();

        dbContext.ClaimAutomationArguments.Add(claimAutomationArgument);
        dbContext.Invoking(db => db.SaveChanges()).Should().Throw<DbUpdateException>();
    }
}
