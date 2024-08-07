namespace API.MeasurementsSyncer.Metrics;

public interface IMeasurementSyncMetrics
{
    void MeasurementsFetched(long fetchedCount);
    void UpdateTimeSinceLastMeasurementSyncerRun(long epochTimeInSeconds);
    void UpdateTimePeriodForSearchingForGSRN(long epochTimeInSeconds);
    void AddMissingMeasurement(long numberOfMissingMeasurements);
    void AddRecoveredMeasurements(long numberOfRecoveredMeasurements);
    void AddNumberOfRecordsBeingSynced(long numberOfRecordsBeingSynced);
}
