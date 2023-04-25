using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
using API.DataSyncSyncer;
using API.DataSyncSyncer.Client;
using API.DataSyncSyncer.Client.Dto;
using API.DataSyncSyncer.Persistence;
using Domain.Certificates.Primitives;
using FluentAssertions;
using MeasurementEvents;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.UnitTests.DataSyncSyncer;

public class DataSyncServiceTest
{
    private readonly CertificateIssuingContract contract = new()
    {
        GSRN = "gsrn",
        GridArea = "gridArea",
        MeteringPointType = MeteringPointType.Production,
        MeteringPointOwner = "meteringPointOwner",
        StartDate = DateTimeOffset.Now.AddDays(-1)
    };

    private readonly Mock<IDataSyncClient> fakeClient = new();
    private readonly Mock<ILogger<DataSyncService>> fakeLogger = new();
    private readonly Mock<ISyncState> fakeSyncState = new();

    [Fact]
    public async Task FetchMeasurements_AfterContractStartDate_DataFetched()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        contract.StartDate = contractStartDate;

        fakeSyncState.Setup(it => it.GetPeriodStartTime(contract))
            .ReturnsAsync(contractStartDate.ToUnixTimeSeconds());

        var fakeResponseList = new List<DataSyncDto>
        {
            new(
                GSRN: contract.GSRN,
                DateFrom: contractStartDate.ToUnixTimeSeconds(),
                DateTo: DateTimeOffset.Now.ToUnixTimeSeconds(),
                Quantity: 5,
                Quality: MeasurementQuality.Measured
            )
        };

        fakeClient.Setup(it => it.RequestAsync(
                contract.GSRN,
                It.IsAny<Period>(),
                contract.MeteringPointOwner,
                CancellationToken.None)
            )
            .ReturnsAsync(() => fakeResponseList);

        var service = SetupService();

        var response = await service.FetchMeasurements(contract,
            CancellationToken.None);

        response.Should().Equal(fakeResponseList);
    }

    [Fact]
    public async Task FetchMeasurements_NoMeasurements_NoDataFetched()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        contract.StartDate = contractStartDate;

        fakeSyncState.Setup(it => it.GetPeriodStartTime(contract))
            .ReturnsAsync(contractStartDate.ToUnixTimeSeconds());

        fakeClient.Setup(it => it.RequestAsync(
                contract.GSRN,
                It.IsAny<Period>(),
                contract.MeteringPointOwner,
                CancellationToken.None)
            )
            .ReturnsAsync(() => new List<DataSyncDto>());

        var service = SetupService();

        var response = await service.FetchMeasurements(contract,
            CancellationToken.None);

        response.Should().BeEmpty();
        fakeClient.Verify(
            c => c.RequestAsync(It.IsAny<string>(), It.IsAny<Period>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FetchMeasurements_BeforeContractStartDate_NoDataFetched()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(1);
        contract.StartDate = contractStartDate;

        fakeSyncState.Setup(it => it.GetPeriodStartTime(contract))
            .ReturnsAsync(contractStartDate.ToUnixTimeSeconds());
        var service = SetupService();

        var response = await service.FetchMeasurements(contract,
            CancellationToken.None);

        response.Should().BeEmpty();
        fakeClient.Verify(
            c => c.RequestAsync(It.IsAny<string>(), It.IsAny<Period>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FetchMeasurements_NoPeriodStartTimeInSyncState_NoDataFetched()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(1);
        contract.StartDate = contractStartDate;

        fakeSyncState.Setup(it => it.GetPeriodStartTime(contract))
            .ReturnsAsync((long?)null);
        var service = SetupService();

        var response = await service.FetchMeasurements(contract,
            CancellationToken.None);

        response.Should().BeEmpty();
        fakeClient.Verify(
            c => c.RequestAsync(It.IsAny<string>(), It.IsAny<Period>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    private DataSyncService SetupService()
    {
        var fakeFactory = new Mock<IDataSyncClientFactory>();
        fakeFactory.Setup(it => it.CreateClient()).Returns(fakeClient.Object);

        return new DataSyncService(
            factory: fakeFactory.Object,
            logger: fakeLogger.Object,
            syncState: fakeSyncState.Object
        );
    }
}
