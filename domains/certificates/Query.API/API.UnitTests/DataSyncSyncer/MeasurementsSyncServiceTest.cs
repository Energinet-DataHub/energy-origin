using System;
using System.Threading;
using System.Threading.Tasks;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Persistence;
using FluentAssertions;
using Measurements.V1;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Testing.Extensions;

namespace API.UnitTests.DataSyncSyncer;

public class MeasurementsSyncServiceTest
{
    private readonly MeteringPointSyncInfo syncInfo = new(
        GSRN: "gsrn",
        StartSyncDate: DateTimeOffset.Now.AddDays(-1),
        MeteringPointOwner: "meteringPointOwner");

    private readonly Measurements.V1.Measurements.MeasurementsClient fakeClient = Substitute.For<Measurements.V1.Measurements.MeasurementsClient>();
    private readonly ILogger<MeasurementsSyncService> fakeLogger = Substitute.For<ILogger<MeasurementsSyncService>>();
    private readonly ISyncState fakeSyncState = Substitute.For<ISyncState>();

    [Fact]
    public async Task FetchMeasurements_AfterContractStartDate_DataFetched()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.GetPeriodStartTime(info)
            .Returns(contractStartDate.ToUnixTimeSeconds());

        var mockResponse = new GetMeasurementsResponse
        {
            Measurements =
            {
                new Measurement
                {
                    Gsrn = info.GSRN,
                    DateFrom = contractStartDate.ToUnixTimeSeconds(),
                    DateTo = DateTimeOffset.Now.ToUnixTimeSeconds(),
                    Quantity = 5,
                    Quality = EnergyQuantityValueQuality.Measured
                }
            }
        };
        fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(mockResponse);

        var service = SetupService();

        var response = await service.FetchMeasurements(info,
            CancellationToken.None);

        response.Should().Equal(mockResponse.Measurements);
    }

    [Fact]
    public async Task FetchMeasurements_NoMeasurements_NoDataFetched()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.GetPeriodStartTime(info)
            .Returns(contractStartDate.ToUnixTimeSeconds());

        fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(new GetMeasurementsResponse());

        var service = SetupService();

        var response = await service.FetchMeasurements(info,
            CancellationToken.None);

        response.Should().BeEmpty();
        _ = fakeClient.Received(1).GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>());
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
        _ = fakeClient.DidNotReceive().GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>());
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
        _ = fakeClient.DidNotReceive().GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>());
    }

    [Fact]
    public async Task FetchMeasurements_MeasurementsReceived_SyncPositionUpdated()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.GetPeriodStartTime(info)
            .Returns(contractStartDate.ToUnixTimeSeconds());

        var dateTo = DateTimeOffset.Now.ToUnixTimeSeconds();
        var mockedResponse = new GetMeasurementsResponse
        {
            Measurements =
            {
                new Measurement
                {
                    Gsrn = info.GSRN,
                    DateFrom = contractStartDate.ToUnixTimeSeconds(),
                    DateTo = dateTo,
                    Quantity = 5,
                    Quality = EnergyQuantityValueQuality.Measured

                }
            }
        };

        fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(mockedResponse);

        var service = SetupService();

        await service.FetchMeasurements(info,
            CancellationToken.None);

        await fakeSyncState.Received(1).SetSyncPosition(Arg.Any<string>(), Arg.Is<long>(x => x == dateTo));
    }

    [Fact]
    public async Task FetchMeasurements_NoMeasurementsReceived_SyncPositionNotUpdated()
    {
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.GetPeriodStartTime(info)
            .Returns(contractStartDate.ToUnixTimeSeconds());

        var mockedResponse = new GetMeasurementsResponse();

        fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(mockedResponse);

        var service = SetupService();

        await service.FetchMeasurements(info,
            CancellationToken.None);

        await fakeSyncState.DidNotReceive().SetSyncPosition(Arg.Any<string>(), Arg.Any<long>());
    }

    private MeasurementsSyncService SetupService()
    {
        return new MeasurementsSyncService(
            logger: fakeLogger,
            syncState: fakeSyncState,
            fakeClient
        );
    }
}
