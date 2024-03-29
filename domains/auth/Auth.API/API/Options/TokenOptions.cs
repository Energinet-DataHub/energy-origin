using System.ComponentModel.DataAnnotations;

namespace API.Options;

public class TokenOptions
{
    public const string Prefix = "Token";

    [Required]
    public string Audience { get; init; } = null!;
    [Range(typeof(TimeSpan), "00:01:00", "24:00:00")]
    public TimeSpan Duration { get; init; }
    [Required]
    public string Issuer { get; init; } = null!;
    [Required]
    public byte[] PrivateKeyPem { get; init; } = null!;
    [Required]
    public byte[] PublicKeyPem { get; init; } = null!;
}
