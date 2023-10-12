namespace MeasurementEvents;

public record ProductionEnergyMeasuredIntegrationEvent(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    MeasurementQuality Quality
);

public record ConsumptionEnergyMeasuredIntegrationEvent(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    MeasurementQuality Quality
);

public enum MeasurementQuality { Measured, Revised, Calculated, Estimated }
