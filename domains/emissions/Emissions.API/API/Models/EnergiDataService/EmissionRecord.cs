using System.Text.Json.Serialization;

namespace API.Models;

public class EmissionRecord
{
    [JsonPropertyName("priceArea")]
    public string GridArea { get; }
    public decimal NOXPerkWh { get; }
    public decimal CO2PerkWh { get; }
    public DateTime HourUTC { get; }

    public EmissionRecord(string gridArea, decimal nOXPerkWh, decimal cO2PerkWh, DateTime hourUTC)
    {
        HourUTC = hourUTC;
        GridArea = gridArea;
        CO2PerkWh = cO2PerkWh;
        NOXPerkWh = nOXPerkWh;
    }
}
