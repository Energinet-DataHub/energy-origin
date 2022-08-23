namespace API.Configuration;

public class AuthOptions
{
    // Token related
    public string InternalTokenSecret { get; set; }

    public string TokenExpiryTimeInDays { get; set; }

    // OIDC Related
    public string Scope { get; set; }
    public string AmrValues { get; set; }
    public string AuthorityUrl { get; set; }
    public string OidcClientId { get; set; }
    public string OidcClientSecret { get; set; }

    // Base url
    public string BaseUrl { get; set; }

    // Cryptography
    public string SecretKey { get; set; }

    // Cookies
    public string CookieName { get; set; }
}
