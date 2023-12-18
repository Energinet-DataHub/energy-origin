using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RichardSzalay.MockHttp;
using TransferAgreementAutomation.Worker;
using TransferAgreementAutomation.Worker.Metrics;
using TransferAgreementAutomation.Worker.Models;
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
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        var memoryCache = new AutomationCache();

        var agreements = new List<TransferAgreementDto>
        {
            new(
                EndDate: DateTimeOffset.Now.AddHours(3).ToUnixTimeSeconds(),
                Id: Guid.NewGuid(),
                ReceiverReference: Guid.NewGuid().ToString(),
                ReceiverTin: "12345678",
                SenderId: Guid.NewGuid().ToString(),
                SenderName: "Peter Producent",
                SenderTin: "11223344",
                StartDate: DateTimeOffset.Now.ToUnixTimeSeconds()
            )
        };

        mockHttpMessageHandler.Expect("/api/transfer-agreements").Respond("application/json",
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
        var httpClient = mockHttpMessageHandler.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:8080");
        httpClient.DefaultRequestHeaders.Add("EO_API_VERSION", "20231123");

        var worker = new TransferAgreementsAutomationWorker(loggerMock, metricsMock, memoryCache, serviceProviderMock,
            httpClient);

        var act = async () => await worker.StartAsync(cts.Token);
        await act.Invoke();
        await Task.Delay(1000); // Give the worker some time to set the cache

        memoryCache.Cache.Get(HealthEntries.Key).Should().Be(HealthEntries.Unhealthy);
    }

    [Fact]
    public async Task WhenCalled_CacheIsSetToHealthy()
    {
        var loggerMock = Substitute.For<ILogger<TransferAgreementsAutomationWorker>>();
        var metricsMock = Substitute.For<ITransferAgreementAutomationMetrics>();
        var poWalletServiceMock = Substitute.For<IProjectOriginWalletService>();
        using var mockHttpMessageHandler = new MockHttpMessageHandler();
        var memoryCache = new AutomationCache();

        var agreements = new List<TransferAgreementDto>
        {
            new(
                EndDate: DateTimeOffset.Now.AddHours(3).ToUnixTimeSeconds(),
                Id: Guid.NewGuid(),
                ReceiverReference: Guid.NewGuid().ToString(),
                ReceiverTin: "12345678",
                SenderId: Guid.NewGuid().ToString(),
                SenderName: "Peter Producent",
                SenderTin: "11223344",
                StartDate: DateTimeOffset.Now.ToUnixTimeSeconds()
            )
        };

        mockHttpMessageHandler.Expect("/api/transfer-agreements").Respond("application/json",
            JsonSerializer.Serialize(new TransferAgreementsDto(agreements)));

        using var cts = new CancellationTokenSource();

        poWalletServiceMock
            .When(x => x.TransferCertificates(Arg.Any<TransferAgreementDto>()))
            .Do(_ => { cts.Cancel(); });

        var serviceProviderMock = SetupIServiceProviderMock(poWalletServiceMock);
        var httpClient = mockHttpMessageHandler.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:8080");
        httpClient.DefaultRequestHeaders.Add("EO_API_VERSION", "20231123");

        var worker = new TransferAgreementsAutomationWorker(loggerMock, metricsMock, memoryCache, serviceProviderMock,
            httpClient);

        var act = async () => await worker.StartAsync(cts.Token);
        await act.Invoke();

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
