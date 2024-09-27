using System.Diagnostics.Metrics;

namespace API.MeasurementsSyncer.Metrics;

public class MeasurementSyncMetrics : IMeasurementSyncMetrics
{
    public const string MetricName = "MeasurementSync";

    private long _numberOfMeasurementsFetched;
    private long _numberOfMeasurementsFetchedCounter;
    private ObservableCounter<long> NumberOfMeasurementsFetchedCounter { get; }
    private long _numberOfMeasurementsFetchedGauge;
    private ObservableGauge<long> NumberOfMeasurementsFetchedGauge { get; }

    private long _numberOfMeasurementsPublished;
    private long _numberOfMeasurementsPublishedCounter;
    private ObservableCounter<long> NumberOfMeasurementsPublishedCounter { get; }
    private long _numberOfMeasurementsPublishedGauge;
    private ObservableGauge<long> NumberOfMeasurementsPublishedGauge { get; }

    private long _numberOfContractsBeingSynced;
    private long _numberOfContractsBeingSyncedCounter;
    private ObservableCounter<long> NumberOfContractsBeingSyncedCounter { get; }
    private long _numberOfContractsBeingSyncedGauge;
    private ObservableGauge<long> NumberOfContractsBeingSyncedGauge { get; }

    private long _numberOfMissingMeasurements;
    private long _numberOfMissingMeasurementsCounter;
    private ObservableCounter<long> NumberOfMissingMeasurementsCounter { get; }
    private long _numberOfMissingMeasurementsGauge;
    private ObservableGauge<long> NumberOfMissingMeasurementsGauge { get; }

    private long _numberOfRecoveredMeasurements;
    private long _numberOfRecoveredMeasurementsCounter;
    private ObservableCounter<long> NumberOfRecoveredMeasurementsCounter { get; }
    private long _numberOfRecoveredMeasurementsGauge;
    private ObservableGauge<long> NumberOfRecoveredMeasurementsGauge { get; }

    private long _numberOfDuplicateMeasurements;
    private long _numberOfDuplicateMeasurementsCounter;
    private ObservableCounter<long> NumberOfDuplicateMeasurementsCounter { get; }
    private long _numberOfDuplicateMeasurementsGauge;
    private ObservableGauge<long> NumberOfDuplicateMeasurementsGauge { get; }

    private long _numberOfMeasurementsQuantityMissingFlag;
    private long _numberOfMeasurementsQuantityMissingFlagCounter;
    private ObservableCounter<long> NumberOfMeasurementsQuantityMissingFlagCounter { get; }
    private long _numberOfMeasurementsQuantityMissingFlagGauge;
    private ObservableGauge<long> NumberOfMeasurementsQuantityMissingFlagGauge { get; }

    private long _numberOfMeasurementsQuantityTooLow;
    private long _numberOfMeasurementsQuantityTooLowCounter;
    private ObservableCounter<long> NumberOfMeasurementsQuantityTooLowCounter { get; }
    private long _numberOfMeasurementsQuantityTooLowGauge;
    private ObservableGauge<long> NumberOfMeasurementsQuantityTooLowGauge { get; }

    private long _numberOfMeasurementsQuantityTooHigh;
    private long _numberOfMeasurementsQuantityTooHighCounter;
    private ObservableCounter<long> NumberOfMeasurementsQuantityTooHighCounter { get; }
    private long _numberOfMeasurementsQuantityTooHighGauge;
    private ObservableGauge<long> NumberOfMeasurementsQuantityTooHighGauge { get; }

    private long _numberOfMeasurementsWrongQuality;
    private long _numberOfMeasurementsWrongQualityCounter;
    private ObservableCounter<long> NumberOfMeasurementsWrongQualityCounter { get; }
    private long _numberOfMeasurementsWrongQualityGauge;
    private ObservableGauge<long> NumberOfMeasurementsWrongQualityGauge { get; }

    public MeasurementSyncMetrics()
    {
        var meter = new Meter(MetricName);

        NumberOfMeasurementsFetchedCounter = meter.CreateObservableCounter("ett_certificate_measurements_fetched", () => _numberOfMeasurementsFetchedCounter);
        NumberOfMeasurementsFetchedGauge = meter.CreateObservableGauge("ett_certificate_measurements_fetched_gauge", () => _numberOfMeasurementsFetchedGauge);

        NumberOfMeasurementsPublishedCounter = meter.CreateObservableCounter("ett_certificate_measurements_published", () => _numberOfMeasurementsPublishedCounter);
        NumberOfMeasurementsPublishedGauge = meter.CreateObservableGauge("ett_certificate_measurements_published_gauge", () => _numberOfMeasurementsPublishedGauge);

        NumberOfContractsBeingSyncedCounter = meter.CreateObservableCounter("ett_certificate_contracts_being_synced", () => _numberOfContractsBeingSyncedCounter);
        NumberOfContractsBeingSyncedGauge = meter.CreateObservableGauge("ett_certificate_contracts_being_synced_gauge", () => _numberOfContractsBeingSyncedGauge);

        NumberOfMissingMeasurementsCounter = meter.CreateObservableCounter("ett_certificate_missing_measurement", () => _numberOfMissingMeasurementsCounter);
        NumberOfMissingMeasurementsGauge = meter.CreateObservableGauge("ett_certificate_missing_measurement_gauge", () => _numberOfMissingMeasurementsGauge);

        NumberOfRecoveredMeasurementsCounter = meter.CreateObservableCounter("ett_certificate_recovered_measurements", () => _numberOfRecoveredMeasurementsCounter);
        NumberOfRecoveredMeasurementsGauge = meter.CreateObservableGauge("ett_certificate_recovered_measurements_gauge", () => _numberOfRecoveredMeasurementsGauge);

        NumberOfDuplicateMeasurementsCounter = meter.CreateObservableCounter("ett_certificate_duplicate_measurements", () => _numberOfDuplicateMeasurementsCounter);
        NumberOfDuplicateMeasurementsGauge = meter.CreateObservableGauge("ett_certificate_duplicate_measurements_gauge", () => _numberOfDuplicateMeasurementsGauge);

        NumberOfMeasurementsQuantityMissingFlagCounter = meter.CreateObservableCounter("ett_certificate_measurement_quantity_missing_flag_count", () => _numberOfMeasurementsQuantityMissingFlagCounter);
        NumberOfMeasurementsQuantityMissingFlagGauge = meter.CreateObservableGauge("ett_certificate_measurement_quantity_missing_flag_gauge", () => _numberOfMeasurementsQuantityMissingFlagGauge);

        NumberOfMeasurementsQuantityTooLowCounter = meter.CreateObservableCounter("ett_certificate_measurement_quantity_too_low_count", () => _numberOfMeasurementsQuantityTooLowCounter);
        NumberOfMeasurementsQuantityTooLowGauge = meter.CreateObservableGauge("ett_certificate_measurement_quantity_too_low_gauge", () => _numberOfMeasurementsQuantityTooLowGauge);

        NumberOfMeasurementsQuantityTooHighCounter = meter.CreateObservableCounter("ett_certificate_measurement_quantity_too_high_count", () => _numberOfMeasurementsQuantityTooHighCounter);
        NumberOfMeasurementsQuantityTooHighGauge = meter.CreateObservableGauge("ett_certificate_measurement_quantity_too_high_gauge", () => _numberOfMeasurementsQuantityTooHighGauge);

        NumberOfMeasurementsWrongQualityCounter = meter.CreateObservableCounter("ett_certificate_measurement_wrong_quality_count", () => _numberOfMeasurementsWrongQualityCounter);
        NumberOfMeasurementsWrongQualityGauge = meter.CreateObservableGauge("ett_certificate_measurement_wrong_quality_gauge", () => _numberOfMeasurementsWrongQualityGauge);
    }

    public void UpdateGauges()
    {
        _numberOfMeasurementsFetchedGauge = _numberOfMeasurementsFetchedCounter - _numberOfMeasurementsFetched;
        _numberOfMeasurementsFetched = _numberOfMeasurementsFetchedCounter;

        _numberOfMeasurementsPublishedGauge = _numberOfMeasurementsPublishedCounter - _numberOfMeasurementsPublished;
        _numberOfMeasurementsPublished = _numberOfMeasurementsPublishedCounter;

        _numberOfContractsBeingSyncedGauge = _numberOfContractsBeingSyncedCounter - _numberOfContractsBeingSynced;
        _numberOfContractsBeingSynced = _numberOfContractsBeingSyncedCounter;

        _numberOfMissingMeasurementsGauge = _numberOfMissingMeasurementsCounter - _numberOfMissingMeasurements;
        _numberOfMissingMeasurements = _numberOfMissingMeasurementsCounter;

        _numberOfRecoveredMeasurementsGauge = _numberOfRecoveredMeasurementsCounter - _numberOfRecoveredMeasurements;
        _numberOfRecoveredMeasurements = _numberOfRecoveredMeasurementsCounter;

        _numberOfDuplicateMeasurementsGauge = _numberOfDuplicateMeasurementsCounter - _numberOfDuplicateMeasurements;
        _numberOfDuplicateMeasurements = _numberOfDuplicateMeasurementsCounter;

        _numberOfMeasurementsQuantityMissingFlagGauge = _numberOfMeasurementsQuantityMissingFlagCounter - _numberOfMeasurementsQuantityMissingFlag;
        _numberOfMeasurementsQuantityMissingFlag = _numberOfMeasurementsQuantityMissingFlagCounter;

        _numberOfMeasurementsQuantityTooLowGauge = _numberOfMeasurementsQuantityTooLowCounter - _numberOfMeasurementsQuantityTooLow;
        _numberOfMeasurementsQuantityTooLow = _numberOfMeasurementsQuantityTooLowCounter;

        _numberOfMeasurementsQuantityTooHighGauge = _numberOfMeasurementsQuantityTooHighCounter - _numberOfMeasurementsQuantityTooHigh;
        _numberOfMeasurementsQuantityTooHigh = _numberOfMeasurementsQuantityTooHighCounter;

        _numberOfMeasurementsWrongQualityGauge = _numberOfMeasurementsWrongQualityCounter - _numberOfMeasurementsWrongQuality;
        _numberOfMeasurementsWrongQuality = _numberOfMeasurementsWrongQualityCounter;
    }

    public void AddNumberOfMeasurementsFetched(long numberOfMeasurementsFetched)
    {
        _numberOfMeasurementsFetchedCounter += numberOfMeasurementsFetched;
    }

    public void AddNumberOfMeasurementsPublished(long numberOfMeasurementsPublished)
    {
        _numberOfMeasurementsPublishedCounter += numberOfMeasurementsPublished;
    }

    public void AddNumberOfMissingMeasurement(long numberOfMissingMeasurements)
    {
        _numberOfMissingMeasurementsCounter += numberOfMissingMeasurements;
    }

    public void AddNumberOfRecoveredMeasurements(long numberOfRecoveredMeasurements)
    {
        _numberOfRecoveredMeasurementsCounter += numberOfRecoveredMeasurements;
    }

    public void AddNumberOfDuplicateMeasurements(long numberOfDuplicateMeasurements)
    {
        _numberOfDuplicateMeasurementsCounter += numberOfDuplicateMeasurements;
    }

    public void AddNumberOfContractsBeingSynced(long numberOfContractsBeingSynced)
    {
        _numberOfContractsBeingSyncedCounter += numberOfContractsBeingSynced;
    }

    public void AddFilterDueQuantityMissingFlag(long numberOfMeasurementsQuantityMissingFlag)
    {
        _numberOfMeasurementsQuantityMissingFlagCounter += numberOfMeasurementsQuantityMissingFlag;
    }

    public void AddFilterDueQuantityTooLow(long numberOfMeasurementsQuantityTooLow)
    {
        _numberOfMeasurementsQuantityTooLowCounter += numberOfMeasurementsQuantityTooLow;
    }

    public void AddFilterDueQuantityTooHigh(long numberOfMeasurementsQuantityTooHigh)
    {
        _numberOfMeasurementsQuantityTooHighCounter += numberOfMeasurementsQuantityTooHigh;
    }

    public void AddFilterDueQuality(long numberOfMeasurementsWrongQuality)
    {
        _numberOfMeasurementsWrongQualityCounter += numberOfMeasurementsWrongQuality;
    }
}
