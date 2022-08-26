using API.Configuration;
using API.Models;
using API.Services;
using API.Services.OidcProviders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

public class LogoutController : ControllerBase
{
    readonly ITokenStorage tokenStorage;
    private readonly IOidcProviders oidcProviders;
    private readonly AuthOptions authOptions;

    public LogoutController(
        ITokenStorage tokenStorage,
        IOptions<AuthOptions> authOptions,
        IOidcProviders oidcProviders
        )
    {
        this.tokenStorage = tokenStorage;
        this.authOptions = authOptions.Value;
        this.oidcProviders = oidcProviders;
    }

    [HttpPost]
    [Route("/auth/logout")]
    public async Task<ActionResult<LogoutResponse>> Logout()
    {
        var opaqueToken = HttpContext.Request.Headers[authOptions.CookieName].FirstOrDefault()?.Split(" ").Last();

        if (opaqueToken != null)
        {
            var idToken = tokenStorage.GetIdTokenByOpaqueToken(opaqueToken);
            await oidcProviders.Logout(idToken);
            tokenStorage.DeleteByOpaqueToken(opaqueToken);
        }

        Response.Cookies.Delete(authOptions.CookieName);

        return Ok(new LogoutResponse { Success = true });
    }
}
