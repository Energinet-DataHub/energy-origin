using System.Text;

namespace EnergyOrigin.IntegrationEvents.Events.EnergyMeasured.V3;

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

public record Address
{
    public string StreetName { get; }
    public string BuildingNumber { get; }
    public string Floor { get; }
    public string Room { get; }
    public string Postcode { get; }
    public string CityName { get; }
    public string Country { get; }


    public Address(string streetName, string buildingNumber, string floor, string room, string postcode, string cityName, string country)
    {
        StreetName = streetName.Trim();
        BuildingNumber = buildingNumber.Trim();
        Floor = floor.Trim();
        Room = room.Trim();
        Postcode = postcode.Trim();
        CityName = cityName.Trim();
        Country = country.Trim();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append($"{StreetName} {BuildingNumber}");

        if (!string.IsNullOrWhiteSpace(Floor))
            sb.Append($", {Floor}");

        if (!string.IsNullOrWhiteSpace(Room))
            sb.Append($". {Room}");

        sb.Append($", {Postcode} {CityName}, {Country}");

        return sb.ToString();
    }
}
