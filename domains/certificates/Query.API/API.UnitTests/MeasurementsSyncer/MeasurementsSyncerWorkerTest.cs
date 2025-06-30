using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using DataContext.ValueObjects;
using EnergyOrigin.Datahub3;
using EnergyOrigin.DatahubFacade;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using Technology = DataContext.ValueObjects.Technology;

namespace API.UnitTests.MeasurementsSyncer;

public class MeasurementsSyncerWorkerTest
{
    private readonly MeteringPointSyncInfo _syncInfo = new(
        Gsrn: Any.Gsrn(),
        StartSyncDate: DateTimeOffset.Now.AddDays(-1),
        EndSyncDate: null,
        MeteringPointOwner: "meteringPointOwner",
        MeteringPointType.Production,
        "DK1",
        Guid.NewGuid(),
        new Technology("T12345", "T54321"),
        false);

    private readonly ILogger<MeasurementsSyncerWorker> _fakeLogger = Substitute.For<ILogger<MeasurementsSyncerWorker>>();
    private readonly ILogger<MeasurementsSyncService> _syncServiceFakeLogger = Substitute.For<ILogger<MeasurementsSyncService>>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly ISlidingWindowState _fakeSlidingWindowState = Substitute.For<ISlidingWindowState>();
    private readonly IContractState _fakeContractState = Substitute.For<IContractState>();
    private readonly IOptions<MeasurementsSyncOptions> _options = Options.Create(Substitute.For<MeasurementsSyncOptions>());
    private readonly MeasurementsSyncerWorker _worker;
    private readonly IMeasurementSyncPublisher _fakeMeasurementPublisher = Substitute.For<IMeasurementSyncPublisher>();
    private readonly Meteringpoint.V1.Meteringpoint.MeteringpointClient _fakeMeteringPointsClient = Substitute.For<Meteringpoint.V1.Meteringpoint.MeteringpointClient>();
    private readonly IMeasurementClient _measurementClient = Substitute.For<IMeasurementClient>();
    private readonly IDataHubFacadeClient _dataHubFacadeClient = Substitute.For<IDataHubFacadeClient>();
    private readonly IContractState _contractState = Substitute.For<IContractState>();

    public MeasurementsSyncerWorkerTest()
    {
        var measurementSyncMetrics = Substitute.For<MeasurementSyncMetrics>();
        var syncService = new MeasurementsSyncService(_syncServiceFakeLogger, _fakeSlidingWindowState, new SlidingWindowService(measurementSyncMetrics),
            new MeasurementSyncMetrics(), _fakeMeasurementPublisher, _fakeMeteringPointsClient, _options, _measurementClient, _dataHubFacadeClient, _contractState);
        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_serviceProvider);
        _serviceProvider.GetService<MeasurementsSyncService>().Returns(syncService);
        _worker = new MeasurementsSyncerWorker(_fakeLogger, _fakeContractState, _options, _scopeFactory);
    }

    [Fact]
    public async Task FetchMeasurements_NoPeriodStartTimeInSyncState_NoDataFetched()
    {
        CancellationTokenSource tokenSource = new CancellationTokenSource();

        // Contract starting yesterday
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = _syncInfo with { StartSyncDate = contractStartDate };

        _fakeContractState.GetSyncInfos(Arg.Any<CancellationToken>())
            .Returns(new[] { info }).AndDoes(_ => tokenSource.Cancel());

        // Run worker and wait for completion or timeout
        var workerTask = _worker.StartAsync(tokenSource.Token);
        await Task.WhenAny(workerTask, Task.Delay(TimeSpan.FromMinutes(2), cancellationToken: TestContext.Current.CancellationToken));

        // Assert no timeout and no data fetched
        Assert.True(workerTask.IsCompleted);
        _ = _measurementClient.DidNotReceive().GetMeasurements(Arg.Any<List<Gsrn>>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }
}
