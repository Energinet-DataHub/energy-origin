using System.Diagnostics.Metrics;
using MassTransit.Futures.Contracts;

namespace API.MeasurementsSyncer.Metrics;

public class MeasurementSyncMetrics : IMeasurementSyncMetrics
{
    public const string MetricName = "MeasurementSync";
    private long totalMeasurementsFetched = 0;
    private ObservableCounter<long> TotalMeasurementsFetched { get; }
    private long timeSinceLastMeasurementSyncerRun = 0;
    private ObservableGauge<long> TimeSinceLastMeasurementSyncerRun { get; }
    private long numberOfContractsBeingSynced = 0;
    private ObservableCounter<long> NumberOfContractsBeingSynced { get; }
    private long timePeriodForSearchingForAGSRN = 0;
    private ObservableGauge<long> TimePeriodForSearchingForAGSRN { get; }
    private long missingMeasurement = 0;
    private ObservableCounter<long> MissingMeasurement { get; }
    private long recoveredMeasurements = 0;
    private ObservableCounter<long> RecoveredMeasurements { get; }

    private long numberOfMeasurementsQuantityTooLow = 0;
    private ObservableCounter<long> NumberOfMeasurementsQuantityTooLow { get; }

    private long numberOfMeasurementsQuantityTooHigh = 0;
    private ObservableCounter<long> NumberOfMeasurementsQuantityTooHigh { get; }

    private long numberOfMeasurementsWrongQuality = 0;
    private ObservableCounter<long> NumberOfMeasurementsWrongQuality { get; }


    public MeasurementSyncMetrics()
    {
        var meter = new Meter(MetricName);
        TimeSinceLastMeasurementSyncerRun = meter.CreateObservableGauge("ett_certificate_time_since_last_measurement_syncer_run", () => timeSinceLastMeasurementSyncerRun, unit: "s");
        TotalMeasurementsFetched = meter.CreateObservableCounter("ett_certificate_measurements_fetched", () => totalMeasurementsFetched);
        NumberOfContractsBeingSynced = meter.CreateObservableCounter("ett_certificate_contracts_being_synced", () => numberOfContractsBeingSynced);
        TimePeriodForSearchingForAGSRN = meter.CreateObservableGauge("ett_certificate_time_period_for_searching_for_gsrn", () => timePeriodForSearchingForAGSRN);
        MissingMeasurement = meter.CreateObservableCounter("ett_certificate_missing_measurement", () => missingMeasurement);
        RecoveredMeasurements = meter.CreateObservableCounter("ett_certificate_recovered_measurements", () => recoveredMeasurements);
        NumberOfMeasurementsQuantityTooLow = meter.CreateObservableCounter("ett_certificate_measurement_quantity_too_low_count", () => numberOfMeasurementsQuantityTooLow);
        NumberOfMeasurementsQuantityTooHigh = meter.CreateObservableCounter("ett_certificate_measurement_quantity_too_high_count", () => numberOfMeasurementsQuantityTooHigh);
        NumberOfMeasurementsWrongQuality = meter.CreateObservableCounter("ett_certificate_measurement_wrong_quality_count", () => numberOfMeasurementsWrongQuality);
    }

    public void MeasurementsFetched(long fetchedCount)
    {
        totalMeasurementsFetched += fetchedCount;
    }

    public void UpdateTimeSinceLastMeasurementSyncerRun(long epochTimeInSeconds)
    {
        timeSinceLastMeasurementSyncerRun = epochTimeInSeconds;
    }

    public void UpdateTimePeriodForSearchingForGSRN(long epochTimeInSeconds)
    {
        timeSinceLastMeasurementSyncerRun = epochTimeInSeconds;
    }

    public void AddMissingMeasurement(long numberOfMissingMeasurements)
    {
        missingMeasurement += numberOfMissingMeasurements;
    }

    public void AddRecoveredMeasurements(long numberOfRecoveredMeasurements)
    {
        recoveredMeasurements += numberOfRecoveredMeasurements;
    }

    public void AddNumberOfRecordsBeingSynced(long numberOfRecordsBeingSynced)
    {
        numberOfContractsBeingSynced += numberOfRecordsBeingSynced;
    }

    public void AddFilterDueQuantityTooLow(long numberOfMeasurementsQuantityTooLow)
    {
        this.numberOfMeasurementsQuantityTooLow += numberOfMeasurementsQuantityTooLow;
    }

    public void AddFilterDueQuantityTooHigh(long numberOfMeasurementsQuantityTooHigh)
    {
        this.numberOfMeasurementsQuantityTooHigh += numberOfMeasurementsQuantityTooHigh;
    }

    public void AddFilterDueQuality(long numberOfMeasurementsWrongQuality)
    {
        this.numberOfMeasurementsWrongQuality += numberOfMeasurementsWrongQuality;
    }
}
