using System.ComponentModel.DataAnnotations;

namespace AdminPortal.Options;

public class ClientUriOptions
{
    public const string Prefix = "Endpoint";

    [Required]
    public string Authorization { get; set; }

    [Required]
    public string Certificates { get; set; }
}
