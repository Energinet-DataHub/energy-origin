using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.DataSyncSyncer;
using API.DataSyncSyncer.Client;
using API.DataSyncSyncer.Client.Dto;
using API.DataSyncSyncer.Persistence;
using CertificateValueObjects;
using FluentAssertions;
using MeasurementEvents;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.UnitTests.DataSyncSyncer;

public class DataSyncServiceTest
{
    private readonly MeteringPointSyncInfo syncInfo = new(
        GSRN: "gsrn",
        StartSyncDate: DateTimeOffset.Now.AddDays(-1),
        MeteringPointOwner: "meteringPointOwner");

    private readonly Mock<IDataSyncClient> fakeClient = new();
    private readonly Mock<ILogger<DataSyncService>> fakeLogger = new();
    private readonly Mock<ISyncState> fakeSyncState = new();

    [Fact]
    public async Task FetchMeasurements_AfterContractStartDate_DataFetched()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.Setup(it => it.GetPeriodStartTime(info))
            .ReturnsAsync(contractStartDate.ToUnixTimeSeconds());

        var fakeResponseList = new List<DataSyncDto>
        {
            new(
                GSRN: info.GSRN,
                DateFrom: contractStartDate.ToUnixTimeSeconds(),
                DateTo: DateTimeOffset.Now.ToUnixTimeSeconds(),
                Quantity: 5,
                Quality: MeasurementQuality.Measured
            )
        };

        fakeClient.Setup(it => it.RequestAsync(
                info.GSRN,
                It.IsAny<Period>(),
                info.MeteringPointOwner,
                CancellationToken.None)
            )
            .ReturnsAsync(() => fakeResponseList);

        var service = SetupService();

        var response = await service.FetchMeasurements(info,
            CancellationToken.None);

        response.Should().Equal(fakeResponseList);
    }

    [Fact]
    public async Task FetchMeasurements_NoMeasurements_NoDataFetched()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.Setup(it => it.GetPeriodStartTime(info))
            .ReturnsAsync(contractStartDate.ToUnixTimeSeconds());

        fakeClient.Setup(it => it.RequestAsync(
                info.GSRN,
                It.IsAny<Period>(),
                info.MeteringPointOwner,
                CancellationToken.None)
            )
            .ReturnsAsync(() => new List<DataSyncDto>());

        var service = SetupService();

        var response = await service.FetchMeasurements(info,
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
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.Setup(it => it.GetPeriodStartTime(info))
            .ReturnsAsync(contractStartDate.ToUnixTimeSeconds());
        var service = SetupService();

        var response = await service.FetchMeasurements(info,
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
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.Setup(it => it.GetPeriodStartTime(info))
            .ReturnsAsync((long?)null);
        var service = SetupService();

        var response = await service.FetchMeasurements(info,
            CancellationToken.None);

        response.Should().BeEmpty();
        fakeClient.Verify(
            c => c.RequestAsync(It.IsAny<string>(), It.IsAny<Period>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FetchMeasurements_MeasurementsReceived_SyncPositionUpdated()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.Setup(it => it.GetPeriodStartTime(info))
            .ReturnsAsync(contractStartDate.ToUnixTimeSeconds());

        var dateTo = DateTimeOffset.Now.ToUnixTimeSeconds();
        var fakeResponseList = new List<DataSyncDto>
        {
            new(
                GSRN: info.GSRN,
                DateFrom: contractStartDate.ToUnixTimeSeconds(),
                DateTo: dateTo,
                Quantity: 5,
                Quality: MeasurementQuality.Measured
            )
        };

        fakeClient.Setup(it => it.RequestAsync(
                info.GSRN,
                It.IsAny<Period>(),
                info.MeteringPointOwner,
                CancellationToken.None)
            )
            .ReturnsAsync(() => fakeResponseList);

        var service = SetupService();

        await service.FetchMeasurements(info,
            CancellationToken.None);

        fakeSyncState.Verify(s => s.SetSyncPosition(It.Is<SyncPosition>(sp => sp.SyncedTo == dateTo)), Times.Once);
    }

    [Fact]
    public async Task FetchMeasurements_NoMeasurementsReceived_SyncPositionNotUpdated()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.Setup(it => it.GetPeriodStartTime(info))
            .ReturnsAsync(contractStartDate.ToUnixTimeSeconds());

        var fakeResponseList = new List<DataSyncDto>();

        fakeClient.Setup(it => it.RequestAsync(
                info.GSRN,
                It.IsAny<Period>(),
                info.MeteringPointOwner,
                CancellationToken.None)
            )
            .ReturnsAsync(() => fakeResponseList);

        var service = SetupService();

        await service.FetchMeasurements(info,
            CancellationToken.None);

        fakeSyncState.Verify(s => s.SetSyncPosition(It.IsAny<SyncPosition>()), Times.Never);
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
