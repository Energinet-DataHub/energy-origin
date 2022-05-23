using System.Text.Json.Serialization;

namespace API.Models;

public class DeclarationProduction
{
    [JsonPropertyName("help")]
    public string Help { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("result")]
    public Result Result { get; set; }
}

public class Field
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
}

public class Record
{
    [JsonPropertyName("ShareTotal")]
    public double ShareTotal { get; set; }

    [JsonPropertyName("HourUTC")]
    public DateTime HourUTC { get; set; }

    [JsonPropertyName("Version")]
    public string Version { get; set; }

    [JsonPropertyName("PriceArea")]
    public string PriceArea { get; set; }

    [JsonPropertyName("ProductionType")]
    public string ProductionType { get; set; }
}

public class Result
{
    [JsonPropertyName("records")]
    public List<Record> Records { get; set; }

    [JsonPropertyName("fields")]
    public List<Field> Fields { get; set; }

    [JsonPropertyName("sql")]
    public string Sql { get; set; }
}

