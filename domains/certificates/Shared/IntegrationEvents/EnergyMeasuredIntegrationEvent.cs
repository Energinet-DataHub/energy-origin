namespace IntegrationEvents;

public record EnergyMeasuredIntegrationEvent(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    MeasurementQuality Quality
);

public enum MeasurementQuality { Measured, Revised, Calculated, Estimated }
