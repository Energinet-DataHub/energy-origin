using System.Security.Claims;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
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
        Metrics metrics,
        IUserDescriptorMapper mapper,
        IUserService userService,
        ITokenIssuer tokenIssuer)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        var versionBypass = false;

        var user = await userService.GetUserByIdAsync(descriptor.Id);

        if (user != null)
        {
            descriptor = mapper.Map(user, descriptor.ProviderType, descriptor.AccessToken!, descriptor.IdentityToken!);

            var scope = User.FindFirstValue(UserClaimName.Scope);

            if (scope!.Contains(UserScopeClaim.NotAcceptedTerms) == false) versionBypass = true;
        }

        metrics.TokenRefreshCounter.Add(
            1,
            new KeyValuePair<string, object?>("UserId", descriptor?.Id),
            new KeyValuePair<string, object?>("CompanyId", descriptor?.CompanyId)
        );

        return Ok(tokenIssuer.Issue(descriptor, versionBypass));
    }
}
