using API.Configuration;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

public class LogoutController : ControllerBase
{
    readonly ITokenStorage _tokenStorage;
    private readonly AuthOptions _authOptions;

    public LogoutController(ITokenStorage tokenStorage, IOptions<AuthOptions> authOptions)
    {
        _tokenStorage = tokenStorage;
        _authOptions = authOptions.Value;
    }

    [HttpPost]
    [Route("/auth/logout")]
    public ActionResult<LogoutResponse> Logout()
    {
        var opaqueToken = HttpContext.Request.Headers[_authOptions.CookieName].FirstOrDefault()?.Split(" ").Last();

        if (opaqueToken != null)
        {
            var idToken = _tokenStorage.GetIdTokenByOpaqueToken(opaqueToken);
            //TODO _oidcProviders.Logout(idToken);
            _tokenStorage.DeleteByOpaqueToken(opaqueToken);
        }

        Response.Cookies.Delete(_authOptions.CookieName);

        return Ok(new LogoutResponse { success = true });
    }
}
