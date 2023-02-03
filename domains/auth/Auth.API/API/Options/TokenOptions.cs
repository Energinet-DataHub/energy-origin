namespace API.Options;

#pragma warning disable CS8618
public class TokenOptions
{
    public const string Prefix = "Token";

    public string Audience { get; init; }
    public string Issuer { get; init; }
    public TimeSpan Duration { get; init; }
    public byte[] PrivateKeyPem { get; init; }
    public byte[] PublicKeyPem { get; init; }
}
