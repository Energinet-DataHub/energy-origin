using System;
using System.Threading;
using System.Threading.Tasks;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using MassTransit;
using Measurements.V1;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Testing.Extensions;
using Xunit;

namespace API.UnitTests.MeasurementsSyncer;

public class MeasurementsSyncServiceTest
{
    private readonly MeteringPointSyncInfo syncInfo = new(
        GSRN: "gsrn",
        StartSyncDate: DateTimeOffset.Now.AddDays(-1),
        MeteringPointOwner: "meteringPointOwner");

    private readonly Measurements.V1.Measurements.MeasurementsClient fakeClient = Substitute.For<Measurements.V1.Measurements.MeasurementsClient>();
    private readonly ILogger<MeasurementsSyncService> fakeLogger = Substitute.For<ILogger<MeasurementsSyncService>>();
    private readonly ISyncState fakeSyncState = Substitute.For<ISyncState>();
    private readonly IBus fakeBus = Substitute.For<IBus>();
    private readonly MeasurementsSyncService service;

    public MeasurementsSyncServiceTest()
    {
        var measurementSyncMetrics = Substitute.For<MeasurementSyncMetrics>();
        service = new MeasurementsSyncService(fakeLogger, fakeSyncState, fakeClient, fakeBus, new SlidingWindowService(measurementSyncMetrics),
            new MeasurementSyncMetrics());
    }

    [Fact]
    public async Task FetchMeasurements_BeforeContractStartDate_NoDataFetched()
    {
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(string.Empty, UnixTimestamp.Now().Add(TimeSpan.FromDays(1)));
        var response = await service.FetchMeasurements(slidingWindow, syncInfo.MeteringPointOwner, UnixTimestamp.Now(), CancellationToken.None);

        response.Should().BeEmpty();
        _ = fakeClient.DidNotReceive().GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>());
    }

    [Fact]
    public async Task FetchMeasurements_MeasurementsReceived_SyncPositionUpdated()
    {
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.GSRN, UnixTimestamp.Create(syncInfo.StartSyncDate));

        var dateTo = UnixTimestamp.Now().RoundToLatestHour().Seconds;
        var mockedResponse = new GetMeasurementsResponse
        {
            Measurements =
            {
                new Measurement
                {
                    Gsrn = syncInfo.GSRN,
                    DateFrom = slidingWindow.SynchronizationPoint.Seconds,
                    DateTo = dateTo,
                    Quantity = 5,
                    Quality = EnergyQuantityValueQuality.Measured
                }
            }
        };

        fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(mockedResponse);

        await service.FetchAndPublishMeasurements(syncInfo.MeteringPointOwner, slidingWindow, CancellationToken.None);
        await fakeSyncState.Received(1)
            .UpdateSlidingWindow(Arg.Is<MeteringPointTimeSeriesSlidingWindow>(t => t.SynchronizationPoint.Seconds == dateTo), CancellationToken.None);
        await fakeSyncState.Received().SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FetchMeasurements_NoMeasurementsReceived_SlidingWindowIsNotUpdated()
    {
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.GSRN, UnixTimestamp.Create(syncInfo.StartSyncDate));

        var mockedResponse = new GetMeasurementsResponse();

        fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(mockedResponse);

        await service.FetchAndPublishMeasurements(syncInfo.MeteringPointOwner, slidingWindow, CancellationToken.None);
        await fakeSyncState.Received(0)
            .UpdateSlidingWindow(Arg.Any<MeteringPointTimeSeriesSlidingWindow>(), Arg.Any<CancellationToken>());
        await fakeSyncState.DidNotReceive().SaveChangesAsync(CancellationToken.None);
    }
}
