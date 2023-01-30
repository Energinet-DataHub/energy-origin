namespace API.Options;

public record OidcOptions(Uri AuthorityUri, TimeSpan CacheDuration, string ClientId, Uri AuthorityCallbackUri, Uri FrontendRedirectUri)
{
    public const string Prefix = "Oidc";
}
