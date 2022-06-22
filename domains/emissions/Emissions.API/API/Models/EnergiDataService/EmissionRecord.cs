using System.Text.Json.Serialization;

public class EmissionRecord
{
    [JsonPropertyName("priceArea")]
    public string GridArea { get; }
    public float NOXPerkWh { get; }
    public float CO2PerkWh { get; }
    public DateTime HourUTC { get; }

    public EmissionRecord(string gridArea, float nOXPerkWh, float cO2PerkWh, DateTime hourUTC)
    {
        GridArea = gridArea;
        NOXPerkWh = nOXPerkWh;
        CO2PerkWh = cO2PerkWh;
        HourUTC = hourUTC;
    }
}