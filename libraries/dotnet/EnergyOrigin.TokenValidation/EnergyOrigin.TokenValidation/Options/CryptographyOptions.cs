using System.ComponentModel.DataAnnotations;

namespace EnergyOrigin.TokenValidation.Options;

public class CryptographyOptions
{
    public const string Prefix = "Cryptography";

    [Required]
    public string Key { get; init; } = null!;
}
