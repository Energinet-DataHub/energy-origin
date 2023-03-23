using System.Security.Claims;
using API.Repositories.Data;
using API.Services.Interfaces;
using API.Utilities.Interfaces;
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
        IUserDescriptorMapper mapper,
        IUserService userService,
        ITokenIssuer tokenIssuer)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        var versionBypass = false;

        if (descriptor.Id is not null)
        {
            var user = await userService.GetUserByIdAsync(descriptor.Id.Value) ?? throw new NullReferenceException($"GetUserByIdAsync() returned null: {descriptor.Id.Value}");
            descriptor = mapper.Map(user, descriptor.ProviderType, descriptor.AccessToken!, descriptor.IdentityToken!);

            var scope = User.FindFirstValue(UserClaimName.Scope);

            if (scope!.Contains(UserScopeClaim.NotAcceptedTerms) == false) versionBypass = true;
        }

        return Ok(tokenIssuer.Issue(descriptor, versionBypass));
    }
}
