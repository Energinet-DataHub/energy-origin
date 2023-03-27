namespace API.Options;

public class TokenOptions
{
    public const string Prefix = "Token";

    public string Audience { get; init; } = null!;
    public TimeSpan Duration { get; init; }
    public string Issuer { get; init; } = null!;
    public byte[] PrivateKeyPem { get; init; } = null!;
    public byte[] PublicKeyPem { get; init; } = null!;
}
