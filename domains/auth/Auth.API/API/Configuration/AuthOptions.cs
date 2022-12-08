namespace API.Configuration;
#nullable disable
public class AuthOptions
{
    // Token related
    public string InternalTokenSecret { get; set; }

    public string TokenExpiryTimeInDays { get; set; }

    // OIDC Related
    public string AmrValues { get; set; }
    public string OidcUrl { get; set; }
    public string OidcClientId { get; set; }
    public string OidcClientSecret { get; set; }

    // Urls
    public string BaseUrl { get; set; }
    public string ServiceUrl { get; set; }
    public string OidcLoginCallbackPath { get; set; }

    // Cryptography
    public string IdTokenSecretKey { get; set; }
    public string StateSecretKey { get; set; }

    // Cookies
    public string CookieName { get; set; }
    public string CookieValue { get; set; }
    public string CookiePath { get; set; }
    public string CookieDomain { get; set; }
    public string CookieHttpOnly { get; set; }
    public string CookieSameSite { get; set; }
    public string CookieSecure { get; set; }
    public int CookieExpiresTimeDelta { get; set; }

    // Terms
    public string TermsMarkdownFolder { get; set; }
}
