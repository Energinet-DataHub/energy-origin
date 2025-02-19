using System.ComponentModel.DataAnnotations;

namespace EnergyOrigin.TokenValidation.b2c;

public class EntraOptions
{
    public const string Prefix = "Entra";

    [Required]
    public string MetadataAddress { get; init; } = null!;

    [Required]
    public string ValidIssuer { get; init; } = null!;

    [Required]
    public string ValidAudience { get; init; } = null!;

    /// <summary>
    /// The client id that is allowed to call the internal endpoints.
    /// </summary>
    [Required]
    public string AllowedClientId { get; init; } = null!;
}
