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
using NSubstitute;
using Xunit;

namespace API.UnitTests.DataSyncSyncer;

public class DataSyncServiceTest
{
    private readonly MeteringPointSyncInfo syncInfo = new(
        GSRN: "gsrn",
        StartSyncDate: DateTimeOffset.Now.AddDays(-1),
        MeteringPointOwner: "meteringPointOwner");

    private readonly IDataSyncClient fakeClient = Substitute.For<IDataSyncClient>();
    private readonly ILogger<DataSyncService> fakeLogger = Substitute.For<ILogger<DataSyncService>>();
    private readonly ISyncState fakeSyncState = Substitute.For<ISyncState>();

    [Fact]
    public async Task FetchMeasurements_AfterContractStartDate_DataFetched()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.GetPeriodStartTime(info)
            .Returns(contractStartDate.ToUnixTimeSeconds());

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

        fakeClient.RequestAsync(string.Empty, default!, default!, default)
            .ReturnsForAnyArgs(fakeResponseList);

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

        fakeSyncState.GetPeriodStartTime(info)
            .Returns(contractStartDate.ToUnixTimeSeconds());

        fakeClient.RequestAsync(string.Empty, default!, default!, default)
            .ReturnsForAnyArgs(new List<DataSyncDto>());

        var service = SetupService();

        var response = await service.FetchMeasurements(info,
            CancellationToken.None);

        response.Should().BeEmpty();
        await fakeClient.Received(1).RequestAsync(Arg.Any<string>(), Arg.Any<Period>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchMeasurements_BeforeContractStartDate_NoDataFetched()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.GetPeriodStartTime(info)
            .Returns(contractStartDate.ToUnixTimeSeconds());

        var service = SetupService();

        var response = await service.FetchMeasurements(info,
            CancellationToken.None);

        response.Should().BeEmpty();
        await fakeClient.DidNotReceive().RequestAsync(Arg.Any<string>(), Arg.Any<Period>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchMeasurements_NoPeriodStartTimeInSyncState_NoDataFetched()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.GetPeriodStartTime(info)
            .Returns((long?)null);

        var service = SetupService();

        var response = await service.FetchMeasurements(info,
            CancellationToken.None);

        response.Should().BeEmpty();
        await fakeClient.DidNotReceive().RequestAsync(Arg.Any<string>(), Arg.Any<Period>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchMeasurements_MeasurementsReceived_SyncPositionUpdated()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.GetPeriodStartTime(info)
            .Returns(contractStartDate.ToUnixTimeSeconds());

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

        fakeClient.RequestAsync(string.Empty, default!, default!, default)
            .ReturnsForAnyArgs(fakeResponseList);
        
        var service = SetupService();

        await service.FetchMeasurements(info,
            CancellationToken.None);

        fakeSyncState.Received(1).SetSyncPosition(Arg.Is<SyncPosition>(sp => sp.SyncedTo == dateTo));
    }

    [Fact]
    public async Task FetchMeasurements_NoMeasurementsReceived_SyncPositionNotUpdated()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.GetPeriodStartTime(info)
            .Returns(contractStartDate.ToUnixTimeSeconds());

        var fakeResponseList = new List<DataSyncDto>();

        fakeClient.RequestAsync(string.Empty, default!, default!, default)
            .ReturnsForAnyArgs(fakeResponseList);
        
        var service = SetupService();

        await service.FetchMeasurements(info,
            CancellationToken.None);

        fakeSyncState.DidNotReceive().SetSyncPosition(Arg.Any<SyncPosition>());
    }

    private DataSyncService SetupService()
    {
        var fakeFactory = Substitute.For<IDataSyncClientFactory>();
        fakeFactory.CreateClient().ReturnsForAnyArgs(fakeClient);

        return new DataSyncService(
            factory: fakeFactory,
            logger: fakeLogger,
            syncState: fakeSyncState
        );
    }
}
