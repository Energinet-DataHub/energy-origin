namespace API.MeasurementsSyncer.Metrics;

public interface IMeasurementSyncMetrics
{
    void MeasurementsFetched(long fetchedCount);
}
