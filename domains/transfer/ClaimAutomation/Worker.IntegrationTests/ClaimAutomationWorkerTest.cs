using DataContext;
using DataContext.Models;
using EnergyOrigin.WalletClient.Models;
using FluentAssertions;
using MassTransit.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Worker.IntegrationTests;

[Collection(IntegrationTestCollection.CollectionName)]
public class ClaimAutomationWorkerTest
{
    private readonly IntegrationTestFixture _integrationTestFixture;

    public ClaimAutomationWorkerTest(IntegrationTestFixture integrationTestFixture)
    {
        _integrationTestFixture = integrationTestFixture;
    }

    [Fact]
    public async Task ClaimAutomationCanStart()
    {
        var taskCompletedSource = new TaskCompletionSource<bool>();
        var workerRunningTask = taskCompletedSource.Task;

        _integrationTestFixture.ClaimAutomationWorker.ProjectOriginWalletClientMock.GetGranularCertificates(Arg.Any<Guid>(),
            Arg.Any<CancellationToken>(), Arg.Any<int>(), Arg.Any<int>())!.Returns(_ =>
        {
            taskCompletedSource.SetCompleted();
            return Task.FromResult(new ResultList<GranularCertificate>()
            { Metadata = new PageInfo() { Count = 0, Limit = 0, Offset = 0, Total = 0 }, Result = new List<GranularCertificate>() });
        });

        await WaitForMigrations();
        await InsertClaimAutomationArgument();

        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
        await Task.WhenAny(timeoutTask, taskCompletedSource.Task);

        workerRunningTask.IsCompleted.Should().BeTrue();
    }

    private async Task InsertClaimAutomationArgument()
    {
        var scopeFactory = _integrationTestFixture.ClaimAutomationWorker.Services.GetService<IServiceScopeFactory>()!;
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()!;
        await dbContext.ClaimAutomationArguments.AddAsync(new ClaimAutomationArgument(Guid.NewGuid(),
            DateTimeOffset.Now.AddDays(-1).ToUniversalTime()));
        await dbContext.SaveChangesAsync();
    }

    private async Task WaitForMigrations()
    {
        var timeout = DateTimeOffset.UtcNow.AddSeconds(10);

        while (DateTimeOffset.UtcNow < timeout)
        {
            var scopeFactory = _integrationTestFixture.ClaimAutomationWorker.Services.GetService<IServiceScopeFactory>()!;
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>()!;
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

            if (pendingMigrations is null || pendingMigrations.Any())
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                continue;
            }

            return;
        }
    }
}
