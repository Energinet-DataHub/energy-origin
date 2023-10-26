using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Shared.Services;
using API.Transfer.Api.Models;
using API.Transfer.Api.Repository;
using API.Transfer.TransferAgreementsAutomation;
using API.Transfer.TransferAgreementsAutomation.Metrics;
using API.Transfer.TransferAgreementsAutomation.Service;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Exception = System.Exception;

namespace API.UnitTests.Transfer.TransferAgreementsAutomation.Service;

public class TransferAgreementsAutomationServiceTest
{
    [Fact]
    public async Task Run_WhenFail_CacheIsSetToUnhealthy()
    {
        var logger = Substitute.For<ILogger<TransferAgreementsAutomationService>>();
        var transferAgreementRepository = Substitute.For<ITransferAgreementRepository>();
        var projectOriginWalletService = Substitute.For<IProjectOriginWalletService>();
        var metrics = Substitute.For<ITransferAgreementAutomationMetrics>();

        var memoryCache = new AutomationCache();
        using var cts = new CancellationTokenSource();

        transferAgreementRepository
            .When(it => it.GetAllTransferAgreements())
            .Do(_ =>
            {
                cts.Cancel();
                throw new Exception();
            });

        var service = new TransferAgreementsAutomationService(logger, transferAgreementRepository,
            projectOriginWalletService, memoryCache, metrics);

        var act = async () => await service.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        memoryCache.Cache.Get(HealthEntries.Key).Should().Be(HealthEntries.Unhealthy);
    }

    [Fact]
    public async Task Run_WhenCalled_CacheIsSetToHealthy()
    {
        var logger = Substitute.For<ILogger<TransferAgreementsAutomationService>>();
        var transferAgreementRepository = Substitute.For<ITransferAgreementRepository>();
        var projectOriginWalletService = Substitute.For<IProjectOriginWalletService>();
        var metrics = Substitute.For<ITransferAgreementAutomationMetrics>();

        var memoryCache = new AutomationCache();
        using var cts = new CancellationTokenSource();

        transferAgreementRepository.GetAllTransferAgreements().Returns(new List<TransferAgreement>()
        {
            new()
            {
                Id = Guid.NewGuid(),
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(10),
                SenderId = Guid.NewGuid(),
                SenderName = "nrgi A/S",
                SenderTin = "44332211",
                ReceiverTin = "12345678",
                ReceiverReference = Guid.NewGuid()
            }
        }).AndDoes(_ => cts.Cancel());

        var service = new TransferAgreementsAutomationService(logger, transferAgreementRepository,
            projectOriginWalletService, memoryCache, metrics);

        var act = async () => await service.Run(cts.Token);
        await act.Should().ThrowAsync<TaskCanceledException>();

        memoryCache.Cache.Get(HealthEntries.Key).Should().Be(HealthEntries.Healthy);
    }
}
