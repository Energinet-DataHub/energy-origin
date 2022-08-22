using API.Configuration;
using Microsoft.Extensions.Options;


namespace API.Services;

public class CookieService : ICookieService
{
    private readonly AuthOptions _authOptions;

    public CookieService(IOptions<AuthOptions> authOptions)
    {
        _authOptions = authOptions.Value;
    }

    public CookieOptions CreateCookieOptions(int CookieExpireDelta)
    {
        CookieOptions cookieOptions = new CookieOptions
        {
            Path = "/",
            Domain = _authOptions.CookieDomain,
            HttpOnly = bool.Parse(_authOptions.CookieHttpOnly),
            SameSite = Enum.Parse<SameSiteMode>(_authOptions.CookieSameSite),
            Secure = bool.Parse(_authOptions.CookieSecure),
            Expires = DateTime.UtcNow.AddHours(CookieExpireDelta),
        };

        return cookieOptions;
    }
}
