using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Data;
using API.Metrics;
using API.Models;
using API.Services;
using API.TransferAgreementsAutomation;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Exception = System.Exception;

namespace API.IntegrationTests.TransferAgreementsAutomation.Service;

public class TransferAgreementsAutomationServiceTest
{
    [Fact]
    public async Task Run_WhenFail_CacheIsSetToError()
    {
        var logger = Substitute.For<ILogger<TransferAgreementsAutomationService>>();
        var transferAgreementRepository = Substitute.For<ITransferAgreementRepository>();
        var projectOriginWalletService = Substitute.For<IProjectOriginWalletService>();
        var metrics = Substitute.For<ITransferAgreementAutomationMetrics>();

        var memoryCache = new StatusCache();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        transferAgreementRepository
            .When(it => it.GetAllTransferAgreements())
            .Do(_ => throw new Exception());

        var service = new TransferAgreementsAutomationService(logger, transferAgreementRepository,
            projectOriginWalletService, memoryCache, metrics);

        service.Run(cts.Token).Should().Throws(new TaskCanceledException());

        memoryCache.Cache.Get(CacheValues.Key).Should().Be(CacheValues.Error);
    }

    [Fact]
    public async Task Run_WhenCalled_CacheIsSetToSuccess()
    {
        var logger = Substitute.For<ILogger<TransferAgreementsAutomationService>>();
        var transferAgreementRepository = Substitute.For<ITransferAgreementRepository>();
        var projectOriginWalletService = Substitute.For<IProjectOriginWalletService>();
        var metrics = Substitute.For<ITransferAgreementAutomationMetrics>();

        var memoryCache = new StatusCache();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

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
        });

        var service = new TransferAgreementsAutomationService(logger, transferAgreementRepository,
            projectOriginWalletService, memoryCache, metrics);

        service.Run(cts.Token).Should().Throws(new TaskCanceledException());

        memoryCache.Cache.Get(CacheValues.Key).Should().Be(CacheValues.Success);
    }
}
