using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using API.Shared.Data;
using API.Transfer.Api.Models;
using API.Transfer.Api.Services;
using API.Transfer.TransferAgreementsAutomation;
using API.Transfer.TransferAgreementsAutomation.Metrics;
using API.Transfer.TransferAgreementsAutomation.Service;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace API.UnitTests.Transfer.TransferAgreementsAutomation.Service;

public class TransferAgreementsAutomationWorkerTests
{
    [Fact]
    public async Task WhenFail_CacheIsSetToUnhealthy()
    {
        var loggerMock = Substitute.For<ILogger<TransferAgreementsAutomationWorker>>();
        var metricsMock = Substitute.For<ITransferAgreementAutomationMetrics>();
        var memoryCache = new AutomationCache();
        var poWalletServiceMock = Substitute.For<IProjectOriginWalletService>();
        var dbContextFactoryMock = Substitute.For<IDbContextFactory<ApplicationDbContext>>();
        var inMemoryOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("inMemoryTest1")
            .Options;

        await using (var context = new ApplicationDbContext(inMemoryOptions))
        {
            context.TransferAgreements.Add(new TransferAgreement
            {
                EndDate = null,
                Id = Guid.NewGuid(),
                ReceiverReference = Guid.NewGuid(),
                ReceiverTin = "12345678",
                SenderId = Guid.NewGuid(),
                SenderName = "SomeCompany",
                SenderTin = "11223344",
                StartDate = DateTimeOffset.UtcNow,
                TransferAgreementNumber = 1
            });

            await context.SaveChangesAsync();
        }

        dbContextFactoryMock.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(new ApplicationDbContext(inMemoryOptions));

        using var cts = new CancellationTokenSource();
        poWalletServiceMock
            .When(x => x.TransferCertificates(Arg.Any<TransferAgreement>()))
            .Do(_ =>
            {
                cts.Cancel();
                throw new Exception();
            });
        var serviceProviderMock = SetupIServiceProviderMock(poWalletServiceMock);

        var worker = new TransferAgreementsAutomationWorker(loggerMock, metricsMock, memoryCache, dbContextFactoryMock, serviceProviderMock);

        var act = async () => await worker.StartAsync(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        memoryCache.Cache.Get(HealthEntries.Key).Should().Be(HealthEntries.Unhealthy);
    }

    [Fact]
    public async Task WhenCalled_CacheIsSetToHealthy()
    {
        var loggerMock = Substitute.For<ILogger<TransferAgreementsAutomationWorker>>();
        var metricsMock = Substitute.For<ITransferAgreementAutomationMetrics>();
        var memoryCache = new AutomationCache();
        var poWalletServiceMock = Substitute.For<IProjectOriginWalletService>();
        var dbContextFactoryMock = Substitute.For<IDbContextFactory<ApplicationDbContext>>();
        var inMemoryOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("inMemoryTest2")
            .Options;

        await using (var context = new ApplicationDbContext(inMemoryOptions))
        {
            context.TransferAgreements.Add(new TransferAgreement
            {
                EndDate = null,
                Id = Guid.NewGuid(),
                ReceiverReference = Guid.NewGuid(),
                ReceiverTin = "12345678",
                SenderId = Guid.NewGuid(),
                SenderName = "SomeCompany",
                SenderTin = "11223344",
                StartDate = DateTimeOffset.UtcNow,
                TransferAgreementNumber = 1
            });

            await context.SaveChangesAsync();
        }

        dbContextFactoryMock.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(new ApplicationDbContext(inMemoryOptions));

        using var cts = new CancellationTokenSource();
        poWalletServiceMock
            .When(x => x.TransferCertificates(Arg.Any<TransferAgreement>()))
            .Do(_ =>
            {
                cts.Cancel();
            });
        var serviceProviderMock = SetupIServiceProviderMock(poWalletServiceMock);

        var worker = new TransferAgreementsAutomationWorker(loggerMock, metricsMock, memoryCache, dbContextFactoryMock, serviceProviderMock);

        var act = async () => await worker.StartAsync(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        memoryCache.Cache.Get(HealthEntries.Key).Should().Be(HealthEntries.Healthy);
    }

    private static IServiceProvider SetupIServiceProviderMock(IProjectOriginWalletService poWalletServiceMock)
    {
        var serviceProviderMock = Substitute.For<IServiceProvider>();
        serviceProviderMock.GetService(typeof(IProjectOriginWalletService)).Returns(poWalletServiceMock);
        var serviceScope = Substitute.For<IServiceScope>();
        serviceScope.ServiceProvider.Returns(serviceProviderMock);
        var serviceScopeFactoryMock = Substitute.For<IServiceScopeFactory>();
        serviceScopeFactoryMock.CreateScope().Returns(serviceScope);
        serviceProviderMock.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactoryMock);

        return serviceProviderMock;
    }
}
