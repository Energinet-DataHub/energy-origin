using System.ComponentModel.DataAnnotations;

namespace AdminPortal.Options;

public class ClientUriOptions
{
    public const string Prefix = "Endpoint";

    [Required]
    public string Authorization { get; set; } = null!;

    [Required]
    public string Certificates { get; set; } = null!;

    [Required]
    public string Measurements { get; set; } = null!;

    [Required]
    public string Transfers { get; set; } = null!;
}
