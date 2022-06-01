using System.Text.Json.Serialization;

namespace API.Models;

public class DeclarationProduction
{
    public string Help { get; set; }

    public bool Success { get; set; }

    public Result Result { get; set; }
}

public class Field
{
    public string Type { get; set; }

    public string Id { get; set; }
}

public class Record
{
    public double ShareTotal { get; set; }

    public DateTime HourUTC { get; set; }

    public string Version { get; set; }

    public string PriceArea { get; set; }

    public string ProductionType { get; set; }
}

public class Result
{
    public List<Record> Records { get; set; }

    public List<Field> Fields { get; set; }

    public string Sql { get; set; }
}

