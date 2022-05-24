using System.Text.Json.Serialization;
// ReSharper disable ClassNeverInstantiated.Global

namespace API.Models;

public class EmissionsResponse
{
    [JsonPropertyName("result")]
    public EmissionsResult Result { get; set; }
}

public class EmissionRecord
{
    public float NOXPerkWh{ get; set; }
    public float CO2PerkWh { get; set; }
    public DateTime HourUTC { get; set; }
}

public class EmissionsResult
{
    [JsonPropertyName("records")]
    public List<EmissionRecord> EmissionRecords { get; set; }
}
