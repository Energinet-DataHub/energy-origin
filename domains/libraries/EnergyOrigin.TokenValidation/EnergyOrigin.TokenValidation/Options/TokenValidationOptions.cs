using System.ComponentModel.DataAnnotations;

namespace EnergyOrigin.TokenValidation.Options;

public class TokenValidationOptions
{
    public const string Prefix = "TokenValidation";

    [Required]
    public byte[] PublicKey { get; init; } = null!;
    [Required]
    public string Issuer { get; init; } = null!;
    [Required]
    public string Audience { get; init; } = null!;
}
