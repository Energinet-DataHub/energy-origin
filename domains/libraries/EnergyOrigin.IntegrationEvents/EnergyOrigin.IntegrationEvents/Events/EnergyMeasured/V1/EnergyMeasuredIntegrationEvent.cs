namespace EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V1;

public record EnergyMeasuredIntegrationEvent(
    string GSRN,
    string Address,
    string GridArea,
    Guid RecipientId,
    long DateFrom,
    long DateTo,
    long Quantity,
    string Capacity,
    Technology Technology,
    MeterType MeterType,
    Quality Quality
);

public enum MeterType
{
    Consumption,
    Production
}

public enum Quality
{
    Measured,
    Revised,
    Calculated,
    Estimated
}

public record Technology(string AibFuelCode, string AibTechCode);
