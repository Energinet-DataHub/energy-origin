namespace MeasurementEvents;

//TODO Remove after PR has been merged
public record EnergyMeasuredIntegrationEvent(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    MeasurementQuality Quality
);

public abstract record EnergyMeasuredIntegrationEventBase(
    string GSRN,
    long DateFrom,
    long DateTo,
    long Quantity,
    MeasurementQuality Quality
);

public record ProductionEnergyMeasuredIntegrationEvent(string GSRN, long DateFrom, long DateTo, long Quantity, MeasurementQuality Quality)
    : EnergyMeasuredIntegrationEventBase(GSRN, DateFrom, DateTo, Quantity, Quality);

public record ConsumptionEnergyMeasuredIntegrationEvent(string GSRN, long DateFrom, long DateTo, long Quantity, MeasurementQuality Quality)
    : EnergyMeasuredIntegrationEventBase(GSRN, DateFrom, DateTo, Quantity, Quality);

public enum MeasurementQuality { Measured, Revised, Calculated, Estimated }
