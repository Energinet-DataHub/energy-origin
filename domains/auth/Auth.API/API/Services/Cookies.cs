using API.Configuration;
using Microsoft.Extensions.Options;


namespace API.Services;

public class Cookies : ICookies
{
    readonly ILogger<Cookies> _logger;
    private readonly AuthOptions _authOptions;

    public Cookies(ILogger<Cookies> logger, IOptions<AuthOptions> authOptions)
    {
        _logger = logger;
        _authOptions = authOptions.Value;
    }

    public CookieOptions CreateCookieOptions(int CookieExpiresTimeDelta)
    {
        CookieOptions cookieOptions = new CookieOptions
        {
            Path = "/",
            Domain = _authOptions.CookieDomain,
            HttpOnly = bool.Parse(_authOptions.CookieHttpOnly),
            SameSite = Enum.Parse<SameSiteMode>(_authOptions.CookieSameSite),
            Secure = bool.Parse(_authOptions.CookieSecure),
            Expires = DateTime.UtcNow.AddHours(CookieExpiresTimeDelta),
        };

        return cookieOptions;
    }
}
