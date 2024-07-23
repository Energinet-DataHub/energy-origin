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

    public MeasurementSyncMetrics()
    {
        var meter = new Meter(MetricName);
        TimeSinceLastMeasurementSyncerRun = meter.CreateObservableGauge("ett.transfer.time_since_last_measurement_syncer_run", () => timeSinceLastMeasurementSyncerRun, unit: "s");
        TotalMeasurementsFetched = meter.CreateObservableCounter("ett.transfer.measurements_fetched", () => totalMeasurementsFetched);
        NumberOfContractsBeingSynced = meter.CreateObservableCounter("ett.transfer.contracts_being_synced", () => numberOfContractsBeingSynced);
        TimePeriodForSearchingForAGSRN = meter.CreateObservableGauge("ett.transfer.time_period_for_searching_for_gsrn", () => timePeriodForSearchingForAGSRN);
        MissingMeasurement = meter.CreateObservableCounter("ett.transfer.missing_measurement", () => missingMeasurement);
        RecoveredMeasurements = meter.CreateObservableCounter("ett.transfer.recovered_measurements", () => recoveredMeasurements);
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
}
