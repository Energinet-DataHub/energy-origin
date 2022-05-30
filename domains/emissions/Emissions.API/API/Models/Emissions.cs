using System.Text.Json.Serialization;

namespace API.Models;

public class Emissions
{
    [JsonPropertyName("dateFrom")]
    public long DateFrom { get; set; }
    [JsonPropertyName("dateTo")]
    public long DateTo { get; set; }
    [JsonPropertyName("total")]
    public Total Total { get; set; }
    [JsonPropertyName("relative")]
    public Relative Relative { get; set; }
}

public class Total
{
    [JsonPropertyName("co2")]
    public float Co2 { get; set; } //g
  
}

public class Relative
{
    [JsonPropertyName("co2")]
    public float Co2 { get; set; } //g/kWh
  
}
