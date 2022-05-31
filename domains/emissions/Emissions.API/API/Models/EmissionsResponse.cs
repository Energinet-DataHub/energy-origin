using System.Text.Json.Serialization;

namespace API.Models;

public class EmissionsResponse
{
    public EmissionsResult Result { get; set; }
}

public class EmissionRecord
{
    [JsonPropertyName("priceArea")]
    public string GridArea { get; set; }
    
    public float NOXPerkWh{ get; set; }
    public float CO2PerkWh { get; set; }
    public DateTime HourUTC { get; set; }
}

public class EmissionsResult
{
    [JsonPropertyName("records")]
    public List<EmissionRecord> EmissionRecords { get; set; }
}
