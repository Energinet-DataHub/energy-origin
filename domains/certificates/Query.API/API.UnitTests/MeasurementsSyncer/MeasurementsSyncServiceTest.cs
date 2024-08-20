using System;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Clients;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using DataContext.Models;
using DataContext.ValueObjects;
using FluentAssertions;
using MassTransit;
using Measurements.V1;
using Meteringpoint.V1;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Testing.Extensions;
using Testing.Helpers;
using Xunit;
using Technology = DataContext.ValueObjects.Technology;

namespace API.UnitTests.MeasurementsSyncer;

public class MeasurementsSyncServiceTest
{
    private readonly MeteringPointSyncInfo syncInfo = new(
        Gsrn: new Gsrn(GsrnHelper.GenerateRandom()),
        StartSyncDate: DateTimeOffset.Now.AddDays(-1),
        MeteringPointOwner: "meteringPointOwner",
        MeteringPointType.Production,
        "DK1",
        Guid.NewGuid(),
        new Technology("T12345", "T54321"));

    private readonly Measurements.V1.Measurements.MeasurementsClient fakeClient = Substitute.For<Measurements.V1.Measurements.MeasurementsClient>();
    private readonly ILogger<MeasurementsSyncService> fakeLogger = Substitute.For<ILogger<MeasurementsSyncService>>();
    private readonly ISlidingWindowState fakeSlidingWindowState = Substitute.For<ISlidingWindowState>();
    private readonly IBus fakeBus = Substitute.For<IBus>();
    private readonly MeasurementsSyncService service;
    private readonly IStampClient fakeStampClient = Substitute.For<IStampClient>();
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient fakeMeteringPointsClient = Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();


    public MeasurementsSyncServiceTest()
    {
        var measurementSyncMetrics = Substitute.For<MeasurementSyncMetrics>();
        service = new MeasurementsSyncService(fakeLogger, fakeSlidingWindowState, fakeClient, fakeBus, new SlidingWindowService(measurementSyncMetrics),
            new MeasurementSyncMetrics(), fakeStampClient, fakeMeteringPointsClient);
    }

    [Fact]
    public async Task FetchMeasurements_BeforeContractStartDate_NoDataFetched()
    {
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(new Gsrn(GsrnHelper.GenerateRandom()), UnixTimestamp.Now().Add(TimeSpan.FromDays(1)));
        var response = await service.FetchMeasurements(slidingWindow, syncInfo.MeteringPointOwner, UnixTimestamp.Now(), CancellationToken.None);

        response.Should().BeEmpty();
        _ = fakeClient.DidNotReceive().GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>());
    }

    [Fact]
    public async Task FetchMeasurements_MeasurementsReceived_SyncPositionUpdated()
    {
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.Gsrn, UnixTimestamp.Create(syncInfo.StartSyncDate));

        var dateTo = UnixTimestamp.Now().RoundToLatestHour().Seconds;
        var mockedResponse = new GetMeasurementsResponse
        {
            Measurements =
            {
                new Measurement
                {
                    Gsrn = syncInfo.Gsrn.Value,
                    DateFrom = slidingWindow.SynchronizationPoint.Seconds,
                    DateTo = dateTo,
                    Quantity = 5,
                    Quality = EnergyQuantityValueQuality.Measured
                }
            }
        };

        var mockedMeteringPointsResponse = new Meteringpoint.V1.MeteringPointsResponse
        {
            MeteringPoints =
            {
                new Meteringpoint.V1.MeteringPoint
                {
                    MeteringPointId = syncInfo.Gsrn.Value,
                    MeteringPointAlias = "alias",
                    ConsumerStartDate = "consumerStartDate",
                    Capacity = "123",
                    BuildingNumber = "buildingNumber",
                    CityName = "cityName",
                    Postcode = "postcode",
                    StreetName = "streetName",
                }
            }
        };

        fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(mockedMeteringPointsResponse);

        fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(mockedResponse);

        await service.FetchAndPublishMeasurements(syncInfo, slidingWindow, CancellationToken.None);
        await fakeSlidingWindowState.Received(1)
            .UpdateSlidingWindow(Arg.Is<MeteringPointTimeSeriesSlidingWindow>(t => t.SynchronizationPoint.Seconds == dateTo), CancellationToken.None);
        await fakeSlidingWindowState.Received().SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FetchMeasurements_NoMeasurementsReceived_SlidingWindowIsNotUpdated()
    {
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.Gsrn, UnixTimestamp.Create(syncInfo.StartSyncDate));

        var mockedMeteringPointsResponse = new Meteringpoint.V1.MeteringPointsResponse
        {
            MeteringPoints =
            {
                new Meteringpoint.V1.MeteringPoint
                {
                    MeteringPointId = syncInfo.Gsrn.Value,
                    MeteringPointAlias = "alias",
                    ConsumerStartDate = "consumerStartDate",
                    Capacity = "123",
                    BuildingNumber = "buildingNumber",
                    CityName = "cityName",
                    Postcode = "postcode",
                    StreetName = "streetName",
                }
            }
        };

        fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>())
            .Returns(mockedMeteringPointsResponse);

        var mockedResponse = new GetMeasurementsResponse();
        fakeClient.GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>())
            .Returns(mockedResponse);

        await service.FetchAndPublishMeasurements(syncInfo, slidingWindow, CancellationToken.None);
        await fakeSlidingWindowState.Received(0)
            .UpdateSlidingWindow(Arg.Any<MeteringPointTimeSeriesSlidingWindow>(), Arg.Any<CancellationToken>());
        await fakeSlidingWindowState.DidNotReceive().SaveChangesAsync(CancellationToken.None);
    }
}
