namespace API.Options;

public class OidcOptions
{
    public const string Prefix = "Oidc";

    public Uri AuthorityUri { get; init; } = null!;
    public TimeSpan CacheDuration { get; init; }
    public string ClientId { get; init; } = null!;
    public string ClientSecret { get; init; } = null!;
    public Uri AuthorityCallbackUri { get; init; } = null!;
    public Uri FrontendRedirectUri { get; init; } = null!;
    public bool AllowRedirection { get; init; }
}
