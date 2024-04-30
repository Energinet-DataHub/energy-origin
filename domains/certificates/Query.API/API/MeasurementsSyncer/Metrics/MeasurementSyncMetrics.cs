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
    private long numberOfContractsBeingSynced = 0;
    private ObservableCounter<long> NumberOfContractsBeingSynced { get; }


    // Time period for searching for a GSRN (it should always be less than 7 days) - Gauge
    private long timePeriodForSearchingForAGSRN = 0;
    private ObservableGauge<long> TimePeriodForSearchingForAGSRN { get; }


    //  Missing measurement - Count
    private long missingMeasurement = 0;
    private ObservableCounter<long> MissingMeasurement { get; }

    // Recovered measurements - Count
    private long recoveredMeasurements = 0;
    private ObservableCounter<long> RecoveredMeasurements { get; }

    public MeasurementSyncMetrics()
    {
        var meter = new Meter(MetricName);
        TimeSinceLastMeasurementSyncerRun = meter.CreateObservableGauge("time_since_last_measurement_syncer_run", () => timeSinceLastMeasurementSyncerRun);
        TotalMeasurementsFetched = meter.CreateObservableCounter("measurements_fetched_total", () => totalMeasurementsFetched);
        NumberOfContractsBeingSynced = meter.CreateObservableCounter("contracts_being_synced_total", () => numberOfContractsBeingSynced);
        TimePeriodForSearchingForAGSRN = meter.CreateObservableGauge("time_period_for_searching_for_gsrn", () => timePeriodForSearchingForAGSRN);
        MissingMeasurement = meter.CreateObservableCounter("missing_measurement_total", () => missingMeasurement);
        RecoveredMeasurements = meter.CreateObservableCounter("recovered_measurements_total", () => recoveredMeasurements);
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
