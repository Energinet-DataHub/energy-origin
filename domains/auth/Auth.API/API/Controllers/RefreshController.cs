using API.Options;
using API.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[Authorize]
[ApiController]
public class RefreshController : ControllerBase
{
    [HttpGet()]
    [Route("auth/refresh")]
    public ActionResult RefreshAccessToken(
        IHttpContextAccessor accessor,
        IOptions<TokenOptions> tokenOptions,
        IUserDescriptMapper descriptMapper,
        ITokenIssuer tokenIssuer)
    {
        var oldDescriptor = descriptMapper.Map(User) ?? throw new NullReferenceException($"UserDescriptMapper failed: {User}");
        var token = tokenIssuer.Issue(oldDescriptor);

        accessor.HttpContext!.Response.Cookies.Append("Authentication", token, new CookieOptions
        {
            IsEssential = true,
            Secure = true,
            Expires = DateTimeOffset.UtcNow.Add(tokenOptions.Value.CookieDuration)
        });

        return NoContent();
    }
}
