using API.Configuration;
using Microsoft.Extensions.Options;

namespace API.Services;

public class Cookies : ICookies
{
    private readonly AuthOptions authOptions;

    public Cookies(IOptions<AuthOptions> authOptions) => this.authOptions = authOptions.Value;

    public CookieOptions CreateCookieOptions(int cookieExpiresTimeDelta)
    {
        var cookieOptions = new CookieOptions
        {
            Path = "/",
            Domain = authOptions.CookieDomain,
            HttpOnly = bool.Parse(authOptions.CookieHttpOnly),
            SameSite = Enum.Parse<SameSiteMode>(authOptions.CookieSameSite),
            Secure = bool.Parse(authOptions.CookieSecure),
            Expires = DateTime.UtcNow.AddHours(cookieExpiresTimeDelta),
        };

        return cookieOptions;
    }
}
