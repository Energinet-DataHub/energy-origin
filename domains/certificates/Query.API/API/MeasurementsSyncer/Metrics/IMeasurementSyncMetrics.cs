namespace API.MeasurementsSyncer.Metrics;

public interface IMeasurementSyncMetrics
{
    void MeasurementsFetched(long fetchedCount);
    void TimeSinceLastMeasurementSyncerRunDone(long epochTimeInSeconds);
    void TimePeriodForSearchingForAGSRNAssign(long epochTimeInSeconds);
}
