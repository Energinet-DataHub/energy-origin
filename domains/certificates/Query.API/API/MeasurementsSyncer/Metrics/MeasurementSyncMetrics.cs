using System.Diagnostics.Metrics;
using MassTransit.Futures.Contracts;

namespace API.MeasurementsSyncer.Metrics;

public class MeasurementSyncMetrics : IMeasurementSyncMetrics
{
    public const string MetricName = "MeasurementSync";

    // Number of measurements fetched by MeasurementSyncer on a sync - Counter
    private long totalMeasurementsFetched = 0;
    private ObservableCounter<long> TotalMeasurementsFetched { get; }

    //  Time since last time MeasurementSyncer attempted to sync anything - Gauge
    private long timeSinceLastMeasurementSyncerRun = 0;
    private ObservableGauge<long> TimeSinceLastMeasurementSyncerRun { get; }

    // Number of contracts/GSRN's being synced by MeasurementSyncer on a sync - Counter



    // Time period for searching for a GSRN (it should always be less than 7 days) - Gauge
    private long timePeriodForSearchingForAGSRN = 0;
    private ObservableGauge<long> TimePeriodForSearchingForAGSRN { get; }


    //  Missing measurement - Count


    // Recovered measurements - Count




    public MeasurementSyncMetrics()
    {
        var meter = new Meter(MetricName);
        TimeSinceLastMeasurementSyncerRun = meter.CreateObservableGauge("time_since_last_measurement_syncer_run", () => timeSinceLastMeasurementSyncerRun);
        TotalMeasurementsFetched = meter.CreateObservableCounter("measurements_fetched_total", () => totalMeasurementsFetched);
    }

    public void MeasurementsFetched(long fetchedCount)
    {
        totalMeasurementsFetched += fetchedCount;
    }

    public void TimeSinceLastMeasurementSyncerRunDone(long epochTimeInSeconds)
    {
        timeSinceLastMeasurementSyncerRun = epochTimeInSeconds;
    }

    public void TimePeriodForSearchingForAGSRNAssign(long epochTimeInSeconds)
    {
        timeSinceLastMeasurementSyncerRun = epochTimeInSeconds;
    }
}
