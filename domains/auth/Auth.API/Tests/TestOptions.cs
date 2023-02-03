
using API.Options;
using Microsoft.Extensions.Options;

namespace Tests;

public static class TestOptions
{
    public static IOptions<OidcOptions> Oidc(
        string authority = "http://them.local/",
        string authorityCallback = "http://api.local",
        string frontendRedirect = "http://us.local",
        string? clientId = default,
        TimeSpan? cacheDuration = default
    ) => Options.Create(new OidcOptions
    {
        AuthorityUri = new Uri(authority),
        AuthorityCallbackUri = new Uri(authorityCallback),
        FrontendRedirectUri = new Uri(frontendRedirect),
        ClientId = clientId ?? Guid.NewGuid().ToString(),
        CacheDuration = cacheDuration ?? new TimeSpan(6, 0, 0)
    });

    public static IOptions<TokenOptions> Token(
        string audience = "Users",
        string issuer = "Us",
        TimeSpan? duration = default,
        byte[]? privateKey = default,
        byte[]? publicKey = default
    ) => Options.Create(new TokenOptions
    {
        Audience = audience,
        Issuer = issuer,
        Duration = duration ?? new TimeSpan(0, 1, 0),
        PrivateKeyPem = privateKey ?? File.ReadAllBytes("./a-private-key.pem")!,
        PublicKeyPem = publicKey ?? File.ReadAllBytes("./a-public-key.pem")!
    });
}
