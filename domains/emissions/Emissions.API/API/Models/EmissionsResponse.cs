 using System.Text.Json.Serialization;

 namespace API.Models;

public class EmissionsResponse
{
    [JsonPropertyName("emissions")]
    public IEnumerable<Emissions> Emissions { get; }

    public EmissionsResponse(IEnumerable<Emissions> emissions)
    {
        Emissions = emissions;
    }
}

public class Emissions
{
    [JsonPropertyName("dateFrom")]
    public long DateFrom { get; }

    [JsonPropertyName("dateTo")]
    public long DateTo { get; }

    [JsonPropertyName("total")]
    public Quantity Total { get; }

    [JsonPropertyName("relative")]
    public Quantity Relative { get; }

    public Emissions(long dateFrom, long dateTo, Quantity total, Quantity relative)
    {
        DateFrom = dateFrom;
        DateTo = dateTo;
        Total = total;
        Relative = relative;
    }
}

public class Total
{
    [JsonPropertyName("co2")]
    public float Co2 { get; } //g

    public Total(float co2)
    {
        Co2 = co2;
    }
}

public class Relative
{
    [JsonPropertyName("co2")]
    public float Co2 { get; set; } //g/kWh

    public Relative(float co2)
    {
        Co2 = co2;
    }
}

