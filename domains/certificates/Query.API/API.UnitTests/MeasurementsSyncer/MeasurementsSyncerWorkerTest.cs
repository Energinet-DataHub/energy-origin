using System;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Metrics;
using API.MeasurementsSyncer.Persistence;
using MassTransit;
using Measurements.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace API.UnitTests.MeasurementsSyncer;

public class MeasurementsSyncerWorkerTest
{
    private readonly MeteringPointSyncInfo syncInfo = new(
        GSRN: "gsrn",
        StartSyncDate: DateTimeOffset.Now.AddDays(-1),
        MeteringPointOwner: "meteringPointOwner");

    private readonly Measurements.V1.Measurements.MeasurementsClient fakeClient = Substitute.For<Measurements.V1.Measurements.MeasurementsClient>();
    private readonly ILogger<MeasurementsSyncerWorker> fakeLogger = Substitute.For<ILogger<MeasurementsSyncerWorker>>();
    private readonly ILogger<MeasurementsSyncService> syncServiceFakeLogger = Substitute.For<ILogger<MeasurementsSyncService>>();
    private readonly IServiceScopeFactory scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceScope scope = Substitute.For<IServiceScope>();
    private readonly IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
    private readonly ISyncState fakeSyncState = Substitute.For<ISyncState>();
    private readonly IContractState fakeContractState = Substitute.For<IContractState>();
    private readonly IBus fakeBus = Substitute.For<IBus>();
    private readonly MeasurementsSyncOptions options = Substitute.For<MeasurementsSyncOptions>();
    private readonly MeasurementsSyncerWorker worker;

    public MeasurementsSyncerWorkerTest()
    {
        var syncService = new MeasurementsSyncService(syncServiceFakeLogger, fakeSyncState, fakeClient, fakeBus, new SlidingWindowService(),
            new MeasurementSyncMetrics());
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(serviceProvider);
        serviceProvider.GetService<MeasurementsSyncService>().Returns(syncService);
        worker = new MeasurementsSyncerWorker(fakeLogger, fakeContractState, Options.Create(options), scopeFactory);
    }

    [Fact]
    public async Task FetchMeasurements_NoPeriodStartTimeInSyncState_NoDataFetched()
    {
        CancellationTokenSource tokenSource = new CancellationTokenSource();

        // Contract starting yesterday
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeContractState.GetSyncInfos(Arg.Any<CancellationToken>())
            .Returns(new[] { info }).AndDoes(c => tokenSource.Cancel());

        // No sync state returned for contract
        fakeSyncState.GetPeriodStartTime(info, CancellationToken.None)
            .Returns((long?)null);

        // Run worker and wait for completion or timeout
        var workerTask = worker.StartAsync(tokenSource.Token);
        await Task.WhenAny(workerTask, Task.Delay(TimeSpan.FromMinutes(2)));

        // Assert no timeout and no data fetched
        Assert.True(workerTask.IsCompleted);
        _ = fakeClient.DidNotReceive().GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>());
    }
}
