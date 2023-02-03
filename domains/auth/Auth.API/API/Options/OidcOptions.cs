namespace API.Options;

#pragma warning disable CS8618
public class OidcOptions
{
    public const string Prefix = "Oidc";

    public Uri AuthorityUri { get; init; }
    public TimeSpan CacheDuration { get; init; }
    public string ClientId { get; init; }
    public Uri AuthorityCallbackUri { get; init; }
    public Uri FrontendRedirectUri { get; init; }
}
