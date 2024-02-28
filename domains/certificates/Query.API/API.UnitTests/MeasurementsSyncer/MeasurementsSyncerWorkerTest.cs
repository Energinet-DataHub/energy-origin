using System;
using System.Threading;
using System.Threading.Tasks;
using API.Configurations;
using API.MeasurementsSyncer;
using API.MeasurementsSyncer.Persistence;
using MassTransit;
using Measurements.V1;
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
    private readonly ISyncState fakeSyncState = Substitute.For<ISyncState>();
    private readonly IBus fakeBus = Substitute.For<IBus>();
    private readonly MeasurementsSyncOptions options = Substitute.For<MeasurementsSyncOptions>();
    private readonly MeasurementsSyncerWorker worker;

    public MeasurementsSyncerWorkerTest()
    {
        var syncService = new MeasurementsSyncService(syncServiceFakeLogger, fakeSyncState, fakeClient, fakeBus, new SlidingWindowService());
        worker = new MeasurementsSyncerWorker(fakeLogger, fakeSyncState, syncService, Options.Create(options));
    }

    [Fact]
    public async Task FetchMeasurements_NoPeriodStartTimeInSyncState_NoDataFetched()
    {
        CancellationTokenSource tokenSource = new CancellationTokenSource();

        // Contract starting yesterday
        var contractStartDate = DateTimeOffset.Now.AddDays(-1);
        var info = syncInfo with { StartSyncDate = contractStartDate };

        fakeSyncState.GetSyncInfos(Arg.Any<CancellationToken>())
            .Returns(new[] { info }).AndDoes(c => tokenSource.Cancel());

        // No sync state returned for contract
        fakeSyncState.GetPeriodStartTime(info)
            .Returns((long?)null);

        // Run worker and wait for completion or timeout
        var workerTask = worker.StartAsync(tokenSource.Token);
        await Task.WhenAny(workerTask, Task.Delay(TimeSpan.FromSeconds(5)));

        // Assert no timeout and no data fetched
        Assert.True(workerTask.IsCompleted);
        fakeClient.DidNotReceive().GetMeasurementsAsync(Arg.Any<GetMeasurementsRequest>());
    }
}
