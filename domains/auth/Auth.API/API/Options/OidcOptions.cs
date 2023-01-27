namespace API.Options;

public record OidcOptions(Uri AuthorityUrl, TimeSpan CacheDuration, string ClientId, string RedirectUri)
{
    public const string Prefix = "Oidc";
}
