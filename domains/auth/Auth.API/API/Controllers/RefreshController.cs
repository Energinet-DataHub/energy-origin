using API.Options;
using API.Services;
using API.Utilities;
using API.Values;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace API.Controllers;

[Authorize]
[ApiController]
public class RefreshController : ControllerBase
{
    [HttpGet()]
    [Route("auth/refresh")]
    public async Task<ActionResult> RefreshAccessToken(
        IHttpClientFactory clientFactory,
        IHttpContextAccessor accessor,
        IOptions<TokenOptions> tokenOptions,
        IUserDescriptMapper descriptMapper,
        IUserService userService,
        IDiscoveryCache discoveryCache,
        ITokenIssuer tokenIssuer,
        IOptions<OidcOptions> oidcOptions,
        ILogger<RefreshController> logger)
    {
        var oldDescriptor = descriptMapper.Map(User) ?? throw new NullReferenceException($"UserDescriptMapper failed: {User}");
        var token = tokenIssuer.Issue(oldDescriptor);

        //var user = await userService.GetUserByIdAsync(oldDescriptor.Id!.Value);
        //var newDescriptor = descriptMapper.Map(user);

        accessor.HttpContext!.Response.Cookies.Append("Authentication", token, new CookieOptions
        {
            IsEssential = true,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.Add(tokenOptions.Value.CookieDuration)
        });

        return NoContent();
    }
}
