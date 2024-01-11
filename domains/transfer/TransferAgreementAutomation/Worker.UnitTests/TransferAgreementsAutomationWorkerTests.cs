using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;
using TransferAgreementAutomation.Worker;
using TransferAgreementAutomation.Worker.Metrics;
using TransferAgreementAutomation.Worker.Models;
using TransferAgreementAutomation.Worker.Options;
using TransferAgreementAutomation.Worker.Service;
using Xunit;

namespace Worker.UnitTests;

public class TransferAgreementsAutomationWorkerTests
{
    [Fact]
    public async Task WhenFail_CacheIsSetToUnhealthy()
    {
        var loggerMock = Substitute.For<ILogger<TransferAgreementsAutomationWorker>>();
        var metricsMock = Substitute.For<ITransferAgreementAutomationMetrics>();
        var poWalletServiceMock = Substitute.For<IProjectOriginWalletService>();
        var httpFactoryMock = Substitute.For<IHttpClientFactory>();
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        var memoryCache = new AutomationCache();

        memoryCache.Cache.Get(HealthEntries.Key).Should().BeNull("Cache should initially be empty");

        var agreements = new List<TransferAgreementDto>
        {
            new(
                EndDate: DateTimeOffset.UtcNow.AddHours(3).ToUnixTimeSeconds(),
                ReceiverReference: Guid.NewGuid().ToString(),
                ReceiverTin: "12345678",
                SenderId: Guid.NewGuid().ToString(),
                StartDate: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            )
        };

        mockHttpMessageHandler.Expect("/api/internal-transfer-agreements/all").Respond("application/json",
            JsonSerializer.Serialize(new TransferAgreementsDto(agreements)));

        var cts = new CancellationTokenSource();

        poWalletServiceMock
            .When(x => x.TransferCertificates(Arg.Any<TransferAgreementDto>()))
            .Do(_ =>
            {
                cts.Cancel();
                cts.Dispose();
                throw new Exception();
            });

        var serviceProviderMock = SetupIServiceProviderMock(poWalletServiceMock);
        httpFactoryMock.CreateClient().Returns(mockHttpMessageHandler.ToHttpClient());
        var options = new TransferApiOptions();
        options.Url = "http://localhost:8080";
        options.Version = "20231123";
        var transferOptions = Options.Create(options);


        var worker = new TransferAgreementsAutomationWorker(
            loggerMock,
            metricsMock,
            memoryCache,
            serviceProviderMock,
            httpFactoryMock,
            transferOptions
        );

        var act = async () => await worker.StartAsync(cts.Token);
        await act.Invoke();
        await Task.Delay(2000); // Give the worker some time to set the cache

        memoryCache.Cache.Get(HealthEntries.Key).Should().Be(HealthEntries.Unhealthy);
    }

    [Fact]
    public async Task WhenCalled_CacheIsSetToHealthy()
    {
        var loggerMock = Substitute.For<ILogger<TransferAgreementsAutomationWorker>>();
        var metricsMock = Substitute.For<ITransferAgreementAutomationMetrics>();
        var poWalletServiceMock = Substitute.For<IProjectOriginWalletService>();
        var httpFactoryMock = Substitute.For<IHttpClientFactory>();
        using var mockHttpMessageHandler = new MockHttpMessageHandler();
        var memoryCache = new AutomationCache();

        var agreements = new List<TransferAgreementDto>
        {
            new(
                EndDate: DateTimeOffset.Now.AddHours(3).ToUnixTimeSeconds(),
                ReceiverReference: Guid.NewGuid().ToString(),
                ReceiverTin: "12345678",
                SenderId: Guid.NewGuid().ToString(),
                StartDate: DateTimeOffset.Now.ToUnixTimeSeconds()
            )
        };

        mockHttpMessageHandler.Expect("/api/internal-transfer-agreements/all").Respond("application/json",
            JsonSerializer.Serialize(new TransferAgreementsDto(agreements)));

        using var cts = new CancellationTokenSource();

        poWalletServiceMock
            .When(x => x.TransferCertificates(Arg.Any<TransferAgreementDto>()))
            .Do(_ =>
            {
                cts.CancelAfter(5); cts.Dispose();
            });

        var serviceProviderMock = SetupIServiceProviderMock(poWalletServiceMock);
        httpFactoryMock.CreateClient().Returns(mockHttpMessageHandler.ToHttpClient());
        var transferOptions = Options.Create(new TransferApiOptions
        {
            Url = "http://localhost:8080",
            Version = "20231123"
        });

        var worker = new TransferAgreementsAutomationWorker(loggerMock, metricsMock, memoryCache, serviceProviderMock,
            httpFactoryMock, transferOptions);

        var act = async () => await worker.StartAsync(cts.Token);
        await act.Invoke();
        await Task.Delay(100);

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
