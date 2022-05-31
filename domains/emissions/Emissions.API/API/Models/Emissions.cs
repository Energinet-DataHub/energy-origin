 namespace API.Models;

public class Emissions
{
    public long DateFrom { get; set; }
    public long DateTo { get; set; }
    public Total Total { get; set; }
    public Relative Relative { get; set; }
}

public class Total
{
    public float Co2 { get; set; } //g
}

public class Relative
{
    public float Co2 { get; set; } //g/kWh
}

