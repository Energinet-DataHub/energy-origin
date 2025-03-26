using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Clients.DataHub3;
using API.MeasurementsSyncer.Clients.DataHubFacade;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using API.Models;
using DataContext.Models;
using DataContext.ValueObjects;
using EnergyOrigin.Domain.ValueObjects;
using FluentAssertions;
using Meteringpoint.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Testing.Extensions;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Technology = DataContext.ValueObjects.Technology;

namespace API.UnitTests.MeasurementsSyncer;

public class MeasurementsSyncServiceTest
{
    private readonly MeteringPointSyncInfo _syncInfo = new(
        Gsrn: Any.Gsrn(),
        StartSyncDate: DateTimeOffset.Now.AddDays(-1),
        EndSyncDate: null,
        MeteringPointOwner: "meteringPointOwner",
        MeteringPointType.Production,
        "DK1",
        Guid.NewGuid(),
        new Technology("T12345", "T54321"));

    private readonly ILogger<MeasurementsSyncService> _fakeLogger = Substitute.For<ILogger<MeasurementsSyncService>>();
    private readonly ISlidingWindowState _fakeSlidingWindowState = Substitute.For<ISlidingWindowState>();
    private readonly IMeasurementSyncPublisher _fakeMeasurementPublisher = Substitute.For<IMeasurementSyncPublisher>();
    private readonly MeasurementsSyncService _service;
    private readonly MeasurementsSyncOptions _options = new();
    private readonly IDataHub3Client _dataHub3Client = Substitute.For<IDataHub3Client>();
    private readonly IDataHubFacadeClient _dataHubFacadeClient = Substitute.For<IDataHubFacadeClient>();
    private readonly IContractState _contractState = Substitute.For<IContractState>();

    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _fakeMeteringPointsClient =
        Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();

    public MeasurementsSyncServiceTest()
    {
        _options.MinimumAgeThresholdHours = 0;
        var measurementSyncMetrics = Substitute.For<MeasurementSyncMetrics>();
        _service = new MeasurementsSyncService(_fakeLogger, _fakeSlidingWindowState,
            new SlidingWindowService(measurementSyncMetrics),
            new MeasurementSyncMetrics(), _fakeMeasurementPublisher, _fakeMeteringPointsClient, Options.Create(_options),
            _dataHub3Client, _dataHubFacadeClient, _contractState);
    }

    [Fact]
    public async Task FetchMeasurements_EmptyList_NoDataFetched()
    {
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(Any.Gsrn(), UnixTimestamp.Now());

        var measurementSyncMetrics = Substitute.For<MeasurementSyncMetrics>();
        _dataHub3Client
            .GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var service = new MeasurementsSyncService(_fakeLogger, _fakeSlidingWindowState,
            new SlidingWindowService(measurementSyncMetrics),
            new MeasurementSyncMetrics(), _fakeMeasurementPublisher, _fakeMeteringPointsClient, Options.Create(_options),
            _dataHub3Client, _dataHubFacadeClient, _contractState);

        var response = await service.FetchMeasurements(slidingWindow, _syncInfo.MeteringPointOwner, UnixTimestamp.Now().AddHours(1), CancellationToken.None);
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task FetchMeasurements_BeforeContractStartDate_NoDataFetched()
    {
        // Given synchronization point
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(Any.Gsrn(), UnixTimestamp.Now().Add(TimeSpan.FromDays(1)));

        // When fetching measurements
        var response = await _service.FetchMeasurements(slidingWindow, _syncInfo.MeteringPointOwner, UnixTimestamp.Now(), CancellationToken.None);

        // Metering point is skipped
        response.Should().BeEmpty();
        _ = _dataHub3Client.DidNotReceive().GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAndPublishMeasurements_WhenNoRelations_NoDataFetched()
    {
        var startSync = UnixTimestamp.Create(_syncInfo.StartSyncDate);
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, startSync);
        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        _ = _dataHub3Client.DidNotReceive().GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAndPublishMeasurements_WhenNullRelations_NoDataFetched()
    {
        var startSync = UnixTimestamp.Create(_syncInfo.StartSyncDate);
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, startSync);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        _ = _dataHub3Client.DidNotReceive().GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAndPublishMeasurements_RelationContainsLmc001Rejection_ContractAndSlidingWindowIsDeleted()
    {
        var startSync = UnixTimestamp.Create(_syncInfo.StartSyncDate);
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, startSync);
        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [],
            Rejections = [new Rejection { ErrorCode = "LMC-001", MeteringPointId = _syncInfo.Gsrn.Value, ErrorDetailName = "SomeDetail", ErrorDetailValue = "SomeValue"}]
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _contractState.Received(1).DeleteContractAndSlidingWindow(Arg.Is<Gsrn>(x => x == _syncInfo.Gsrn));
        await _dataHub3Client.DidNotReceive().GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _fakeSlidingWindowState.DidNotReceive().UpsertSlidingWindow(Arg.Any<MeteringPointTimeSeriesSlidingWindow>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchMeasurements_MeasurementsReceived_SyncPositionUpdated()
    {
        // Given synchronization point
        var startSync = UnixTimestamp.Create(_syncInfo.StartSyncDate);
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, startSync);

        // When measurement is received
        var dateTo = UnixTimestamp.Now().RoundToLatestHour().EpochSeconds;
        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, startSync.EpochSeconds, dateTo, 123);

        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);
        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [new() { MeteringPointId = _syncInfo.Gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        // Then sliding window is updated
        await _fakeSlidingWindowState.Received(1)
            .UpsertSlidingWindow(Arg.Is<MeteringPointTimeSeriesSlidingWindow>(t => t.SynchronizationPoint.EpochSeconds == dateTo),
                CancellationToken.None);
        await _fakeSlidingWindowState.Received().SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FetchMeasurements_NoMeasurementsReceived_SlidingWindowIsNotUpdated()
    {
        // Given synchronization point
        var startSync = UnixTimestamp.Create(_syncInfo.StartSyncDate);
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, startSync);

        // When no measurements fetched
        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: TestContext.Current.CancellationToken).Returns(meteringPointsResponse);

        var dateTo = UnixTimestamp.Now().RoundToLatestHour().EpochSeconds;
        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, startSync.EpochSeconds, dateTo, 123);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        // Then sliding window is not updated
        await _fakeSlidingWindowState.Received(0).UpsertSlidingWindow(Arg.Any<MeteringPointTimeSeriesSlidingWindow>(), Arg.Any<CancellationToken>());
        await _fakeSlidingWindowState.DidNotReceive().SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FetchMeasurements_MeasurementsReceived_MeasurementEventsArePublished()
    {
        // Given synchronization point
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, UnixTimestamp.Create(_syncInfo.StartSyncDate));

        // When 2 measurements where fetched
        var dateFrom = slidingWindow.SynchronizationPoint.EpochSeconds;
        var pa1 = Any.PointAggregation(dateFrom, 5);
        var pa2 = Any.PointAggregation(dateFrom + 3600, 7);
        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, [pa1, pa2]);

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);

        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);
        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [new() { MeteringPointId = _syncInfo.Gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        // Then 2 measurements are published
        await _fakeMeasurementPublisher.Received().PublishIntegrationEvents(Arg.Any<Meteringpoint.V1.MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Is<List<Measurement>>(measurements => measurements.Count == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddingAgeRequirementDoesNotFiltersOutMeasurementsWithinMinimumAgeThreshold()
    {
        _options.MinimumAgeThresholdHours = 100;
        var syncPositionFromLastRun = UnixTimestamp.Now().Add(TimeSpan.FromHours(-24)).RoundToLatestHour();
        var missingIntervals = MeasurementInterval.Create(syncPositionFromLastRun.Add(TimeSpan.FromHours(-200)),
            syncPositionFromLastRun.Add(TimeSpan.FromHours(-199)));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, syncPositionFromLastRun, [missingIntervals]);

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);

        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, [Any.PointAggregation(missingIntervals.From.EpochSeconds, 5)]);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);
        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [new() { MeteringPointId = _syncInfo.Gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<Meteringpoint.V1.MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenMeasurementOutsideMinimumAgeThreshold_WhenCallingFetchAndPublishMeasurements_DoNotPublishMeasurement()
    {
        _options.MinimumAgeThresholdHours = 100;

        // Given sliding window with missing interval
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var syncPoint = now.AddHours(-24);
        var missingIntervals = MeasurementInterval.Create(syncPoint.AddHours(-200), syncPoint.AddHours(-199));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, syncPoint, [missingIntervals]);

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);

        // When getting measurement later than sync point (in the future)
        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, [Any.PointAggregation(now.AddHours(1).EpochSeconds, 5)]);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        // Then measurement is filtered
        await _fakeMeasurementPublisher.DidNotReceive().PublishIntegrationEvents(
            Arg.Any<Meteringpoint.V1.MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenMeasurementsWithinAndOutsideThresholdOnlyPublishMeasurementWithinThreshold()
    {
        _options.MinimumAgeThresholdHours = 100;

        var now = UnixTimestamp.Now().RoundToLatestHour();
        var syncPoint = now.AddHours(-24);
        var missingInterval = MeasurementInterval.Create(syncPoint.AddHours(-200), syncPoint.AddHours(-199));

        var syncInfo = new MeteringPointSyncInfo(
            Gsrn: Any.Gsrn(),
            StartSyncDate: missingInterval.From.ToDateTimeOffset(),
            EndSyncDate: null,
            MeteringPointOwner: "meteringPointOwner",
            MeteringPointType.Production,
            "DK1",
            Guid.NewGuid(),
            new Technology("T12345", "T54321"));

        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(
            syncInfo.Gsrn,
            syncPoint,
            [missingInterval]);

        var meteringPointsResponse = Any.MeteringPointsResponse(syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);

        var paOutsideThreshold = Any.PointAggregation(now.AddHours(-10).EpochSeconds, 7);
        var paWithinThreshold = Any.PointAggregation(missingInterval.From.EpochSeconds, 5);

        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(syncInfo.Gsrn, [paOutsideThreshold, paWithinThreshold]);

        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);
        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [new() { MeteringPointId = syncInfo.Gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);

        await _service.FetchAndPublishMeasurements(syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<Meteringpoint.V1.MeteringPoint>(),
            Arg.Any<MeteringPointSyncInfo>(),
            Arg.Is<List<Measurement>>(measurements =>
                measurements.Count == 1 &&
                measurements.Single().DateFrom == paWithinThreshold.MinObservationTime &&
                measurements.Single().DateTo == (paWithinThreshold.MinObservationTime + 3600)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddingAgeRequirement_DoesNotPublishAlreadyPublishedMeasurements()
    {
        _options.MinimumAgeThresholdHours = 2;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var slidingWindowSyncPoint = now.Add(TimeSpan.FromHours(-4));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, slidingWindowSyncPoint);

        var pa1 = Any.PointAggregation(slidingWindowSyncPoint.EpochSeconds, 5);
        var timeSeriesApiResponse1 = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, [pa1]);

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);

        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse1);
        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [new() { MeteringPointId = _syncInfo.Gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<Meteringpoint.V1.MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());

        _fakeMeasurementPublisher.ClearReceivedCalls();
        _dataHub3Client.ClearReceivedCalls();

        _options.MinimumAgeThresholdHours = 20;

        var pa2 = Any.PointAggregation(slidingWindowSyncPoint.AddHours(1).EpochSeconds, 5);
        var timeSeriesApiResponse2 = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, [pa2]);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse2);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.DidNotReceive().PublishIntegrationEvents(
            Arg.Any<Meteringpoint.V1.MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemovingMinimumAgeRestriction_AllowsFetchingOfMeasurementsPreviouslyExcludedByHigherAgeRestriction()
    {
        _options.MinimumAgeThresholdHours = 5;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, now.Add(TimeSpan.FromHours(-10)));

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);

        var initialResponse = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, []);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(initialResponse);

        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [new() { MeteringPointId = _syncInfo.Gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        _options.MinimumAgeThresholdHours = 0;
        var pa = Any.PointAggregation(now.Add(TimeSpan.FromHours(-4)).EpochSeconds, 10);
        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, [pa]);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<Meteringpoint.V1.MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyingMinimumAgeRestriction_DoesNotFetchPreviouslyFetchedMeasurementsWithinAgeRange()
    {
        var now = UnixTimestamp.Now().RoundToLatestHour();

        var syncPoint = now.Add(TimeSpan.FromHours(1));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, syncPoint);

        var pa = Any.PointAggregation(now.Add(TimeSpan.FromHours(-2)).EpochSeconds, 10);
        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, [pa]);
        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);

        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: TestContext.Current.CancellationToken).Returns(meteringPointsResponse);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        _options.MinimumAgeThresholdHours = 150;

        var response = await _service.FetchMeasurements(slidingWindow, _syncInfo.MeteringPointOwner, now.Add(TimeSpan.FromHours(-150)),
            CancellationToken.None);

        response.Should().BeEmpty();
    }

    [Fact]
    public async Task DecreasingMinimumAge_AllowsFetchingOfMeasurementsPreviouslyExcludedByHigherAgeRestriction()
    {
        _options.MinimumAgeThresholdHours = 5;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, now.Add(TimeSpan.FromHours(-10)));

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);

        var emptyResponse = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, []);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(emptyResponse);

        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [new() { MeteringPointId = _syncInfo.Gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.DidNotReceive().PublishIntegrationEvents(
            Arg.Any<Meteringpoint.V1.MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());

        _dataHub3Client.ClearReceivedCalls();
        _fakeMeasurementPublisher.ClearReceivedCalls();

        _options.MinimumAgeThresholdHours = 2;
        var pa = Any.PointAggregation(now.Add(TimeSpan.FromHours(-3)).EpochSeconds, 10);
        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, [pa]);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);

        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<Meteringpoint.V1.MeteringPoint>(), Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistingMissingIntervals_AdheresToNewlyAddedAgeRestriction()
    {
        var syncInfo = new MeteringPointSyncInfo(
            Gsrn: Any.Gsrn(),
            StartSyncDate: DateTimeOffset.Now.AddDays(-14),
            EndSyncDate: null,
            MeteringPointOwner: "meteringPointOwner",
            MeteringPointType.Production,
            "DK1",
            Guid.NewGuid(),
            new Technology("T12345", "T54321"));

        var slidingWindow =
            MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.Gsrn, UnixTimestamp.Create(syncInfo.StartSyncDate));

        var dateFrom = slidingWindow.SynchronizationPoint.EpochSeconds;
        var pa1 = Any.PointAggregation(dateFrom, 5);

        var pa2 = Any.PointAggregation(dateFrom - TimeSpan.FromHours(200).Seconds, 7);
        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(syncInfo.Gsrn, [pa1, pa2]);
        var meteringPointsResponse = Any.MeteringPointsResponse(syncInfo.Gsrn);

        _options.MinimumAgeThresholdHours = 168;

        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);

        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [new() { MeteringPointId = syncInfo.Gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);
        await _service.FetchAndPublishMeasurements(syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<Meteringpoint.V1.MeteringPoint>(),
            Arg.Any<MeteringPointSyncInfo>(),
            Arg.Do<List<Measurement>>(measurements =>
            {
                measurements.Should().HaveCount(1);
                measurements[0].DateFrom.Should().Be(pa2.MinObservationTime);
                measurements[0].DateTo.Should().Be(pa2.MinObservationTime + 3600);
                measurements[0].Quantity.Should().Be(pa2.AggregatedQuantity + 3600);
            }),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissingMeasurementsOutsideOfAgeThreshold_AreNotProcessed()
    {
        _options.MinimumAgeThresholdHours = 96;
        var now = UnixTimestamp.Now().RoundToLatestHour();

        var syncPoint = now.Add(TimeSpan.FromDays(-7));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, syncPoint);

        var missingIntervalOutsideThreshold = MeasurementInterval.Create(now.Add(TimeSpan.FromHours(-1)), now);
        slidingWindow.MissingMeasurements.Intervals.Add(missingIntervalOutsideThreshold);

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);

        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, [Any.PointAggregation(missingIntervalOutsideThreshold.From.EpochSeconds, 5)]);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);

        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [new() { MeteringPointId = _syncInfo.Gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.DidNotReceive().PublishIntegrationEvents(
            Arg.Any<Meteringpoint.V1.MeteringPoint>(),
            Arg.Any<MeteringPointSyncInfo>(),
            Arg.Any<List<Measurement>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GivenContractAndSlidingWindow_WhenFetchingMeasurements_PointInTimeToSyncToHandlesContractEnd()
    {
        // Given contract and sliding window
        _options.MinimumAgeThresholdHours = 5;
        var latestHour = UnixTimestamp.Now().RoundToLatestHour();
        var contractStart = latestHour.AddHours(-7);
        var contractEnd = latestHour.AddHours(-6);
        var syncInfo = new MeteringPointSyncInfo(
            Gsrn: Any.Gsrn(),
            StartSyncDate: contractStart.ToDateTimeOffset(),
            EndSyncDate: contractEnd.ToDateTimeOffset(),
            MeteringPointOwner: "meteringPointOwner",
            MeteringPointType.Production,
            "DK1",
            Guid.NewGuid(),
            new Technology("T12345", "T54321"));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.Gsrn, contractStart);

        // When fetching measurements
        var meteringPointsResponse = Any.MeteringPointsResponse(syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);

        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(syncInfo.Gsrn, []);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);

        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [new() { MeteringPointId = syncInfo.Gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);
        await _service.FetchAndPublishMeasurements(syncInfo, slidingWindow, CancellationToken.None);

        // Then fetch to contract end (and not all the way to now-age)
        var dateFrom = (long)_dataHub3Client.ReceivedWithAnyArgs(1).ReceivedCalls().First().GetArguments()[1]!;
        var dateTo = (long)_dataHub3Client.ReceivedWithAnyArgs(1).ReceivedCalls().First().GetArguments()[2]!;
        dateFrom.Should().Be(contractStart.EpochSeconds);
        dateTo.Should().Be(contractEnd.EpochSeconds);
    }

    [Fact]
    public async Task GivenContractAndSlidingWindow_WhenFetchingMeasurements_PointInTimeToSyncToHandlesContractWithNoEndDate()
    {
        // Given contract and sliding window
        _options.MinimumAgeThresholdHours = 5;
        var latestHour = UnixTimestamp.Now().RoundToLatestHour();
        var contractStart = latestHour.AddHours(-7);
        var syncInfo = new MeteringPointSyncInfo(
            Gsrn: Any.Gsrn(),
            StartSyncDate: contractStart.ToDateTimeOffset(),
            EndSyncDate: null,
            MeteringPointOwner: "meteringPointOwner",
            MeteringPointType.Production,
            "DK1",
            Guid.NewGuid(),
            new Technology("T12345", "T54321"));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.Gsrn, contractStart);

        // When fetching measurements
        var meteringPointsResponse = Any.MeteringPointsResponse(syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);

        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(syncInfo.Gsrn, []);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);

        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [new() { MeteringPointId = syncInfo.Gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);
        await _service.FetchAndPublishMeasurements(syncInfo, slidingWindow, CancellationToken.None);

        // Then fetch to contract end (and not all the way to now-age)
        var dateFrom = (long)_dataHub3Client.ReceivedWithAnyArgs(1).ReceivedCalls().First().GetArguments()[1]!;
        var dateTo = (long)_dataHub3Client.ReceivedWithAnyArgs(1).ReceivedCalls().First().GetArguments()[2]!;
        dateFrom.Should().Be(contractStart.EpochSeconds);
        dateTo.Should().Be(latestHour.AddHours(-_options.MinimumAgeThresholdHours).EpochSeconds);
    }

    [Fact]
    public async Task
        GivenSingleMissingIntervalSpanning7Days_WhenApplying4DayThreshold_OnlyMeasurementsWithinThresholdAreProcessed_AndRemainingDaysAreStillMissing()
    {
        _options.MinimumAgeThresholdHours = 96;
        var now = UnixTimestamp.Now().RoundToLatestHour();

        var syncStart = now.Add(TimeSpan.FromDays(-7));
        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(_syncInfo.Gsrn, syncStart);

        var missingInterval = MeasurementInterval.Create(syncStart, now);
        slidingWindow.MissingMeasurements.Intervals.Add(missingInterval);

        var meteringPointsResponse = Any.MeteringPointsResponse(_syncInfo.Gsrn);
        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);

        var measurementsWithinThreshold = new List<PointAggregation>();
        var currentTime = syncStart;
        for (var i = 0; i < 168; i++)
        {
            measurementsWithinThreshold.Add(Any.PointAggregation(currentTime.EpochSeconds, 10));
            currentTime = currentTime.Add(TimeSpan.FromHours(1));
        }

        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, measurementsWithinThreshold);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);

        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [new() { MeteringPointId = _syncInfo.Gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);
        await _service.FetchAndPublishMeasurements(_syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<Meteringpoint.V1.MeteringPoint>(),
            Arg.Any<MeteringPointSyncInfo>(),
            Arg.Do<List<Measurement>>(publishedMeasurements =>
            {
                publishedMeasurements.Should().HaveCount(96);
                publishedMeasurements.Should().BeEquivalentTo(measurementsWithinThreshold);
                publishedMeasurements.Should().OnlyContain(m =>
                    m.DateFrom >= now.Add(TimeSpan.FromDays(-4)).EpochSeconds &&
                    m.DateTo <= now.EpochSeconds
                );
            }), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task
        Given_That_We_Have_7days_MissingInterval_And_We_Place_The_AgeThreshold_In_The_Middle_Of_It_Then_Prove_That_Only_MissingIntervals_From_The_SlidingWindows_StartPosition_And_Until_The_AgeThreshold_Are_Being_Processed_Thus_Leaving_The_MissingIntervals_Spanning_From_AgeThresholds_PointInTime_To_CurrentTimeStamp()
    {
        _options.MinimumAgeThresholdHours = 96;
        var now = UnixTimestamp.Now().RoundToLatestHour();
        var syncStart = now.Add(TimeSpan.FromDays(-7)).RoundToLatestHour();

        var syncInfo = new MeteringPointSyncInfo(
            Gsrn: Any.Gsrn(),
            StartSyncDate: syncStart.ToDateTimeOffset(),
            EndSyncDate: null,
            MeteringPointOwner: "meteringPointOwner",
            MeteringPointType.Production,
            "DK1",
            Guid.NewGuid(),
            new Technology("T12345", "T54321"));

        var slidingWindow = MeteringPointTimeSeriesSlidingWindow.Create(syncInfo.Gsrn, syncStart);

        var missingInterval = MeasurementInterval.Create(syncStart, now);
        slidingWindow.MissingMeasurements.Intervals.Add(missingInterval);

        var measurementsWithinThreshold = new List<PointAggregation>();
        var currentTime = syncStart;
        for (int i = 0; i < 96; i++)
        {
            measurementsWithinThreshold.Add(Any.PointAggregation(currentTime.EpochSeconds, 10));
            currentTime = currentTime.Add(TimeSpan.FromHours(1));
        }

        var timeSeriesApiResponse = Any.TimeSeriesApiResponse(_syncInfo.Gsrn, measurementsWithinThreshold);
        var meteringPointsResponse = Any.MeteringPointsResponse(syncInfo.Gsrn);

        _fakeMeteringPointsClient.GetOwnedMeteringPointsAsync(Arg.Any<OwnedMeteringPointsRequest>(), cancellationToken: Arg.Any<CancellationToken>()).Returns(meteringPointsResponse);
        _dataHub3Client.GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(timeSeriesApiResponse);

        var relationsResponse = new ListMeteringPointForCustomerCaResponse
        {
            Relations = [new() { MeteringPointId = syncInfo.Gsrn.Value, ValidFromDate = DateTime.Now.AddHours(-1) }],
            Rejections = []
        };
        _dataHubFacadeClient.ListCustomerRelations(Arg.Any<string>(), Arg.Any<List<Gsrn>>(), Arg.Any<CancellationToken>()).Returns(relationsResponse);
        await _service.FetchAndPublishMeasurements(syncInfo, slidingWindow, CancellationToken.None);

        await _fakeMeasurementPublisher.Received(1).PublishIntegrationEvents(
            Arg.Any<Meteringpoint.V1.MeteringPoint>(),
            Arg.Any<MeteringPointSyncInfo>(),
            Arg.Do<List<Measurement>>(publishedMeasurements =>
            {
                publishedMeasurements.Should().HaveCount(96);
                publishedMeasurements.Should().BeEquivalentTo(measurementsWithinThreshold);
                publishedMeasurements.Should().OnlyContain(m =>
                    m.DateFrom >= now.Add(TimeSpan.FromDays(-4)).EpochSeconds &&
                    m.DateTo <= now.EpochSeconds
                );
            }), Arg.Any<CancellationToken>());
    }
}
