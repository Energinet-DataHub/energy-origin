using System.Diagnostics.Metrics;

namespace API.MeasurementsSyncer.Metrics;

public class MeasurementSyncMetrics : IMeasurementSyncMetrics
{
    public const string MetricName = "MeasurementSync";

    private long totalMeasurementsFetched = 0;
    private ObservableCounter<long> TotalMeasurementsFetched { get; }

    public MeasurementSyncMetrics()
    {
        var meter = new Meter(MetricName);

        TotalMeasurementsFetched = meter.CreateObservableCounter<long>("measurements_fetched_total", () => totalMeasurementsFetched);
    }

    public void MeasurementsFetched(long fetchedCount)
    {
        totalMeasurementsFetched += fetchedCount;
    }
}
