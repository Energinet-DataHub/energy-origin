using API.Configuration;
using API.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
public class TokenController : ControllerBase
{
    private readonly ITokenStorage tokenStorage;
    private readonly AuthOptions authOptions;

    public TokenController(IOptions<AuthOptions> authOptions, ITokenStorage tokenStorage)
    {
        this.authOptions = authOptions.Value;
        this.tokenStorage = tokenStorage;
    }

    [HttpGet]
    [Route("/token/forward-auth")]
    public ActionResult ForwardAuth()
    {
        var opaqueToken = HttpContext.Request.Cookies[authOptions.CookieName];
        if (string.IsNullOrWhiteSpace(opaqueToken))
        {
            return Unauthorized();
        }

        var internalToken = tokenStorage.GetInteralTokenByOpaqueToken(opaqueToken);

        if (internalToken == null)
        {
            return Unauthorized();
        }

        HttpContext.Response.Headers.Add("Authorization", $"Bearer: {internalToken}");

        return Ok();
    }
}
