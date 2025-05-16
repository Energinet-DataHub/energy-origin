using System.ComponentModel.DataAnnotations;

namespace EnergyOrigin.DatahubFacade;

public class DataHubFacadeOptions
{
    public const string Prefix = "DataHubFacade";

    [Required]
    public string Url { get; set; } = "";

    [Required]
    public string GrpcUrl { get; set; } = "";
}
