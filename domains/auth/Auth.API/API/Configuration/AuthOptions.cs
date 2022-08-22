namespace API.Configuration;

public class AuthOptions
{
    // Token related
    public string InternalTokenSecret { get; set; }
    public string TokenExpiryTime { get; set; }

    // OIDC Related
    public string Scope { get; set; }
    public string AmrValues { get; set; }
    public string OidcUrl { get; set; }
    public string OidcClientId { get; set; }
    public string OidcClientSecret { get; set; }

    // Base url
    public string BaseUrl { get; set; }

    // Cryptography
    public string SecretKey { get; set; }

    // Cookies
    public string CookieName { get; set; }
    public string CookieValue { get; set; }
    public string CookiePath { get; set; }
    public string CookieDomain { get; set; }
    public string CookieHttpOnly { get; set; }
    public string CookieSameSite { get; set; }
    public string CookieSecure { get; set; }

}
