using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class DataHubFacadeOptions
{
    public const string Prefix = "DataHubFacade";

    [Required]
    public string Url { get; set; } = "";

    public bool CallRelationService { get; set; } = false;
}
