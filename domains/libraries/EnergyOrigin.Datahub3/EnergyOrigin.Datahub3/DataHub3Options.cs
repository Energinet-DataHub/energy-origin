using System.ComponentModel.DataAnnotations;

namespace EnergyOrigin.Datahub3;

public class DataHub3Options
{
    public const string Prefix = "DataHub3";

    [Required]
    public string Url { get; set; } = "";
    public string? TokenUrl { get; set; } = null!;


    public string? ClientId { get; set; } = null!;
    public string? ClientSecret { get; set; } = null!;
    public string? Scope { get; set; } = null!;

    [Required]
    public bool EnableMock { get; set; } = false;
}
