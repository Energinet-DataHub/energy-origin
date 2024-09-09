namespace API.MeasurementsSyncer.Metrics;

public interface IMeasurementSyncMetrics
{
    void UpdateGauges();

    void AddNumberOfMeasurementsFetched(long numberOfMeasurementsFetched);

    void AddNumberOfMeasurementsPublished(long numberOfMeasurementsPublished);

    void UpdateTimeSinceLastMeasurementSyncerRun(long epochTimeInSeconds);

    void UpdateTimePeriodForSearchingForGSRN(long epochTimeInSeconds);

    void AddNumberOfMissingMeasurement(long numberOfMissingMeasurements);

    void AddNumberOfRecoveredMeasurements(long numberOfRecoveredMeasurements);

    void AddNumberOfContractsBeingSynced(long numberOfContractsBeingSynced);

    void AddFilterDueQuantityTooLow(long numberOfMeasurementsQuantityTooLow);

    void AddFilterDueQuantityTooHigh(long numberOfMeasurementsQuantityTooHigh);

    void AddFilterDueQuality(long numberOfMeasurementsWrongQuality);
}
