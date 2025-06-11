using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using API.IntegrationTests.Setup.Fixtures;
using API.Transfer.Api._Features_;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace API.IntegrationTests.Transfer.Api._Features_;

[Collection(IntegrationTestCollection.CollectionName)]
public class DeleteClaimAutomationArgsCommandTests
{
    private readonly DbContextOptions<ApplicationDbContext> _contextOptions;

    public DeleteClaimAutomationArgsCommandTests(IntegrationTestFixture integrationTestFixture)
    {
        var databaseInfo = integrationTestFixture.PostgresContainer.CreateNewDatabase().GetAwaiter().GetResult();
        new DbMigrator(databaseInfo.ConnectionString, typeof(ApplicationDbContext).Assembly, NullLogger<DbMigrator>.Instance).MigrateAsync().Wait();
        _contextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(databaseInfo.ConnectionString).Options;
    }

    [Fact]
    public async Task DeleteClaimAutomationArgsCommand()
    {
        var cancellationToken = new CancellationToken();
        var subject = Guid.NewGuid();
        var claimArg1 = new ClaimAutomationArgument(subject, DateTimeOffset.Now.ToUniversalTime());
        var claimArg2 = new ClaimAutomationArgument(Guid.NewGuid(), DateTimeOffset.Now.ToUniversalTime());

        await using var dbContext = new ApplicationDbContext(_contextOptions);
        dbContext.ClaimAutomationArguments.Add(claimArg1);
        dbContext.ClaimAutomationArguments.Add(claimArg2);
        await dbContext.SaveChangesAsync(cancellationToken);

        var sut = new DeleteClaimAutomationArgsCommandHandler(dbContext);

        var request = new DeleteClaimAutomationArgsCommand(OrganizationId.Create(subject));
        await sut.Handle(request, cancellationToken);

        var argsDb = dbContext.ClaimAutomationArguments.ToList();

        Assert.Single(argsDb);
        Assert.Equal(claimArg2, argsDb.First());
    }
}
