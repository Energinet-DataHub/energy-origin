using API.Configuration;
using API.Models;
using API.Repository;
using API.Services.OidcProviders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

public class LogoutController : ControllerBase
{
    readonly ITokenStorage tokenStorage;
    private readonly IOidcService oidcService;
    private readonly AuthOptions authOptions;

    public LogoutController(
        ITokenStorage tokenStorage,
        IOptions<AuthOptions> authOptions,
        IOidcService oidcService
        )
    {
        this.tokenStorage = tokenStorage;
        this.authOptions = authOptions.Value;
        this.oidcService = oidcService;
    }

    [HttpPost]
    [Route("/auth/logout")]
    public async Task<ActionResult<LogoutResponse>> Logout()
    {
        var opaqueToken = HttpContext.Request.Cookies[authOptions.CookieName];
        if (opaqueToken != null)
        {
            var idToken = tokenStorage.GetIdTokenByOpaqueToken(opaqueToken);
            await oidcService.Logout(idToken);
            tokenStorage.DeleteByOpaqueToken(opaqueToken);
        }

        Response.Cookies.Delete(authOptions.CookieName);

        return Ok(new LogoutResponse { Success = true });
    }
}
