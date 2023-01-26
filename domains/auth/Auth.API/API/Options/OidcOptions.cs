namespace API.Options;

public class OidcOptions
{
    public const string Prefix = "Oidc";       
    public Uri AuthorityUrl { get; set; }
    public TimeSpan CacheDuration { get; set; }
    public string ClientId { get; set; }
    public string RedirectUri { get; set; }

}
