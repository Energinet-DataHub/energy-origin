using System.Text.Json.Serialization;

namespace API.Shared.EnergiDataService.Models;

public class EmissionRecord
{
    [JsonPropertyName("priceArea")]
    public string GridArea { get; }
    public decimal NOXPerkWh { get; }
    public decimal CO2PerkWh { get; }
    public DateTimeOffset HourUTC { get; }

    public EmissionRecord(string gridArea, decimal nOXPerkWh, decimal cO2PerkWh, DateTimeOffset hourUTC)
    {
        HourUTC = hourUTC;
        GridArea = gridArea;
        CO2PerkWh = cO2PerkWh;
        NOXPerkWh = nOXPerkWh;
    }
}
