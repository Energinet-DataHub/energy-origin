using API.Configuration;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;


namespace API.Services;

public class CookieService : ICookieService
{
    readonly ILogger<CookieService> _logger;
    private readonly HttpContext _httpContext;
    private readonly AuthOptions _authOptions;

    public CookieService(ILogger<CookieService> logger, HttpContext httpContext, IOptions<AuthOptions> authOptions)
    {
        _logger = logger;
        _authOptions = authOptions.Value;
        _httpContext = httpContext;
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

    public string IsValid()
    {
        var cookie = _httpContext.Request.Cookies[_authOptions.CookieName].ToString();

       // var validateCookie = Validator.Validate(cookie);

        //if (opaqueToken == null)
        //{
        //    return Unauthorized();
        //}
        var oue = "test";

        return oue;
    }

    public class Validator : AbstractValidator<CookieOptions>
    {
        public Validator()
        {
            RuleFor(a => a.Domain).NotEmpty();
            RuleFor(a => a.Expires).GreaterThan(DateTimeOffset.Now);
        }
    }
}


    //def is_valid(self) -> 'TokenQuery':
    //    """Check if the token has a correct issued and expires datetime."""

    //    return self.filter(and_(
    //        DbToken.issued <= func.now(),
    //        DbToken.expires > func.now(),
    //    ))
