using System.Text.Json.Serialization;

namespace API.Models;

public class ProductionEmission
{
    public string Help { get; }

    public bool Success { get; }

    public Result Result { get; }

    public ProductionEmission(string help, bool success, Result result)
    {
        Help = help;
        Success = success;
        Result = result;
    }
}
