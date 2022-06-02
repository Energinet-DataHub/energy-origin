using System.Text.Json.Serialization;

namespace API.Models;

public class EmissionsDataResponse
{
    public EmissionsResult Result { get; }

    public EmissionsDataResponse(EmissionsResult result)
    {
        Result = result;
    }
}

public class EmissionRecord
{
    [JsonPropertyName("priceArea")]
    public string GridArea { get; }
    public float NOXPerkWh{ get; }
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

public class EmissionsResult
{
    [JsonPropertyName("records")]
    public List<EmissionRecord> EmissionRecords { get; }

    public EmissionsResult(List<EmissionRecord> emissionRecords)
    {
        EmissionRecords = emissionRecords;
    }
}
