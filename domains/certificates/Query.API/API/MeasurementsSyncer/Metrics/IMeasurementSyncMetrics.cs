namespace API.MeasurementsSyncer.Metrics;

public interface IMeasurementSyncMetrics
{
    void UpdateGauges();

    void AddNumberOfMeasurementsFetched(long numberOfMeasurementsFetched);

    void AddNumberOfMeasurementsPublished(long numberOfMeasurementsPublished);

    void AddNumberOfMissingMeasurement(long numberOfMissingMeasurements);

    void AddNumberOfRecoveredMeasurements(long numberOfRecoveredMeasurements);

    void AddNumberOfDuplicateMeasurements(long numberOfDuplicateMeasurements);

    void AddNumberOfContractsBeingSynced(long numberOfContractsBeingSynced);

    void AddFilterDueQuantityMissingFlag(long numberOfMeasurementsQuantityMissingFlag);

    void AddFilterDueQuantityTooLow(long numberOfMeasurementsQuantityTooLow);

    void AddFilterDueQuantityTooHigh(long numberOfMeasurementsQuantityTooHigh);

    void AddFilterDueQuality(long numberOfMeasurementsWrongQuality);
}
