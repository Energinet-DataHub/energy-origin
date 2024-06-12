using System.ComponentModel.DataAnnotations;

namespace EnergyOrigin.TokenValidation.b2c;

public class B2COptions
{
    public const string Prefix = "B2C";

    [Required]
    public string B2CWellKnownUrl { get; init; } = null!;

    [Required]
    public string ClientCredentialsCustomPolicyWellKnownUrl { get; init; } = null!;

    [Required]
    public string MitIDCustomPolicyWellKnownUrl { get; init; } = null!;

    [Required]
    public string CustomPolicyClientId { get; init; } = null!;
}
