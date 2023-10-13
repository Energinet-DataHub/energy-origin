namespace MeasurementEvents;

public abstract record EnergyMeasuredIntegrationEvent(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    MeasurementQuality Quality
);

public record ProductionEnergyMeasuredIntegrationEvent(string GSRN, long DateFrom, long DateTo, long Quantity, MeasurementQuality Quality)
    : EnergyMeasuredIntegrationEvent(GSRN, DateFrom, DateTo, Quantity, Quality);

public record ConsumptionEnergyMeasuredIntegrationEvent(string GSRN, long DateFrom, long DateTo, long Quantity, MeasurementQuality Quality)
    : EnergyMeasuredIntegrationEvent(GSRN, DateFrom, DateTo, Quantity, Quality);

public enum MeasurementQuality { Measured, Revised, Calculated, Estimated }
