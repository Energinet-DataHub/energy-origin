using API.Options;

namespace Unit.Tests;

public static class TestOptions
{
    public static OidcOptions Oidc(
        OidcOptions options,
        string? authority = default,
        string? authorityCallback = default,
        string? frontendRedirect = default,
        string? clientId = default,
        TimeSpan? cacheDuration = default,
        bool? allowRedirection = default,
        bool? reuseSubject = default
    ) => new()
    {
        AuthorityUri = new Uri(authority ?? options.AuthorityUri.AbsoluteUri),
        AuthorityCallbackUri = new Uri(authorityCallback ?? options.AuthorityCallbackUri.AbsoluteUri),
        FrontendRedirectUri = new Uri(frontendRedirect ?? options.FrontendRedirectUri.AbsoluteUri),
        ClientId = clientId ?? options.ClientId,
        CacheDuration = cacheDuration ?? options.CacheDuration,
        RedirectionMode = allowRedirection ?? options.AllowRedirection ? OidcOptions.Redirection.Allow : OidcOptions.Redirection.Deny,
        IdGeneration = reuseSubject ?? options.ReuseSubject ? OidcOptions.Generation.Predictable : OidcOptions.Generation.Random
    };

    public static TokenOptions Token(
        TokenOptions options,
        string? audience = default,
        string? issuer = default,
        TimeSpan? duration = default
    ) => new()
    {
        Audience = audience ?? options.Audience,
        Issuer = issuer ?? options.Issuer,
        Duration = duration ?? options.Duration,
        PrivateKeyPem = options.PrivateKeyPem,
        PublicKeyPem = options.PublicKeyPem
    };
}
