namespace EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V2;

public record EnergyMeasuredIntegrationEvent(
    string GSRN,
    Address Address,
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

public record Address(string StreetName, string BuildingNumber, string CityName, string Postcode, string Country)
{
    public override string ToString()
    {
        return $"{StreetName} {BuildingNumber}. {Postcode} {CityName}, {Country}";
    }
}
