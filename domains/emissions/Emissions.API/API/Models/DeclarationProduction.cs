using System.Text.Json.Serialization;

namespace API.Models;

public class DeclarationProduction
{
    public string Help { get; }

    public bool Success { get; }

    public Result Result { get; }

    public DeclarationProduction(string help, bool success, Result result)
    {
        Help = help;
        Success = success;
        Result = result;
    }
}

public class Field
{
    public string Type { get; }

    public string Id { get; }

    public Field(string type, string id)
    {
        Type = type;
        Id = id;
    }
}

public class Record
{
    public double ShareTotal { get; }

    public DateTime HourUTC { get; }

    public string Version { get; }

    public string PriceArea { get; }

    public string ProductionType { get; }

    public Record(double shareTotal, DateTime hourUTC, string version, string priceArea, string productionType)
    {
        ShareTotal = shareTotal;
        HourUTC = hourUTC;
        Version = version;
        PriceArea = priceArea;
        ProductionType = productionType;
    }
}

public class Result
{
    public List<Record> Records { get; }

    public List<Field> Fields { get; }

    public string Sql { get; }

    public Result(List<Record> records, List<Field> fields, string sql)
    {
        Records = records;
        Fields = fields;
        Sql = sql;
    }
}

