using System;
using System.Collections.Generic;
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
        var dbContextMock = Substitute.For<ApplicationDbContext>();

        dbContextFactoryMock.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(dbContextMock);

        using var cts = new CancellationTokenSource();
        poWalletServiceMock
            .When(x => x.TransferCertificates(Arg.Any<TransferAgreement>()))
            .Do(_ =>
            {
                cts.Cancel();
                throw new Exception();
            });


        var worker = new TransferAgreementsAutomationWorker(loggerMock, metricsMock, memoryCache, poWalletServiceMock, dbContextFactoryMock);

        var act = async () => await worker.StartAsync(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        memoryCache.Cache.Get(HealthEntries.Key).Should().Be(HealthEntries.Unhealthy);
    }

    //[Fact]
    //public async Task WhenCalled_CacheIsSetToHealthy()
    //{
    //    var loggerMock = Substitute.For<ILogger<TransferAgreementsAutomationWorker>>();
    //    var metricsMock = Substitute.For<ITransferAgreementAutomationMetrics>();
    //    var memoryCache = new AutomationCache();
    //    var taAutomationMock = Substitute.For<ITransferAgreementsAutomationService>();

    //    using var cts = new CancellationTokenSource();
    //    taAutomationMock
    //        .When(x => x.Run(Arg.Any<CancellationToken>()))
    //        .Do(_ =>
    //        {
    //            cts.Cancel();
    //        });

    //    var serviceProviderMock = SetupIServiceProviderMock(taAutomationMock);

    //    var worker = new TransferAgreementsAutomationWorker(loggerMock, serviceProviderMock, metricsMock, memoryCache);

    //    var act = async () => await worker.StartAsync(cts.Token);
    //    await act.Should().ThrowAsync<TaskCanceledException>();

    //    memoryCache.Cache.Get(HealthEntries.Key).Should().Be(HealthEntries.Healthy);
    //}

    private static IServiceProvider SetupIServiceProviderMock(ITransferAgreementsAutomationService taAutomationMock)
    {
        var serviceProviderMock = Substitute.For<IServiceProvider>();
        serviceProviderMock.GetService(typeof(ITransferAgreementsAutomationService)).Returns(taAutomationMock);
        var serviceScope = Substitute.For<IServiceScope>();
        serviceScope.ServiceProvider.Returns(serviceProviderMock);
        var serviceScopeFactoryMock = Substitute.For<IServiceScopeFactory>();
        serviceScopeFactoryMock.CreateScope().Returns(serviceScope);
        serviceProviderMock.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactoryMock);

        return serviceProviderMock;
    }
}
