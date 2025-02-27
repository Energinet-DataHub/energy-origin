using System.ComponentModel.DataAnnotations;

namespace AdminPortal.Options;

public class OidcOptions
{
    public const string Prefix = "OpenIDConnectSettings";

    [Required]
    public string Authority { get; set; } = null!;

    [Required]
    public string ClientId { get; set; } = null!;

    [Required]
    public string ClientSecret { get; set; } = null!;
}
