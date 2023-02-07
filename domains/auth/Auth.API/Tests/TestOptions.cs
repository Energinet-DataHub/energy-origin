
using API.Options;
using Microsoft.Extensions.Options;

namespace Tests;

public static class TestOptions
{
    public static IOptions<OidcOptions> Oidc(
        OidcOptions options,
        string? authority = default,
        string? authorityCallback = default,
        string? frontendRedirect = default,
        string? clientId = default,
        TimeSpan? cacheDuration = default
    ) => Options.Create(new OidcOptions
    {
        AuthorityUri = new Uri(authority ?? options.AuthorityUri.AbsoluteUri),
        AuthorityCallbackUri = new Uri(authorityCallback ?? options.AuthorityCallbackUri.AbsoluteUri),
        FrontendRedirectUri = new Uri(frontendRedirect ?? options.FrontendRedirectUri.AbsoluteUri),
        ClientId = clientId ?? options.ClientId,
        CacheDuration = cacheDuration ?? options.CacheDuration
    });

    public static IOptions<TermsOptions> Terms(TermsOptions options, int? version = null) => Options.Create(new TermsOptions
    {
        CurrentVersion = version ?? options.CurrentVersion,
    });

    public static IOptions<TokenOptions> Token(
        TokenOptions options,
        string? audience = default,
        string? issuer = default,
        TimeSpan? duration = default
    ) => Options.Create(new TokenOptions
    {
        Audience = audience ?? options.Audience,
        Issuer = issuer ?? options.Issuer,
        Duration = duration ?? options.Duration,
        PrivateKeyPem = options.PrivateKeyPem,
        PublicKeyPem = options.PublicKeyPem
    });
}
