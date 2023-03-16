using System.Security.Claims;
using API.Services;
using API.Utilities;
using API.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TokenController : ControllerBase
{
    [HttpGet()]
    [Route("auth/token")]
    public async Task<IActionResult> RefreshAsync(
        IClaimsWrapperMapper claimsWrapperMapper,
        IUserService userService,
        ITokenIssuer tokenIssuer)
    {
        var claimsWrapper = claimsWrapperMapper.Map(User) ?? throw new NullReferenceException($"ClaimsWrapperMapper failed: {User}");
        var versionBypass = false;

        if (claimsWrapper.Id is not null)
        {
            var user = await userService.GetUserByIdAsync(claimsWrapper.Id.Value) ?? throw new NullReferenceException($"GetUserByIdAsync() returned null: {claimsWrapper.Id.Value}");
            claimsWrapper = claimsWrapperMapper.Map(user, claimsWrapper.AccessToken!, claimsWrapper.IdentityToken!);

            var scope = User.FindFirstValue(UserClaimName.Scope);

            if (scope!.Contains(UserScopeClaim.NotAcceptedTerms) == false) versionBypass = true;
        }

        return Ok(tokenIssuer.Issue(claimsWrapper, versionBypass));
    }
}
