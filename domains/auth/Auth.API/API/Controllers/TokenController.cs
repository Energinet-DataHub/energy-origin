using API.Models.Entities;
using API.Options;
using API.Services;
using API.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TokenController : ControllerBase
{
    [HttpGet()]
    [Route("auth/token")]
    public async Task<IActionResult> RefreshAsync(
        IUserDescriptMapper descriptMapper,
        IUserService userService,
        ITokenIssuer tokenIssuer)
    {
        var descriptor = descriptMapper.Map(User) ?? throw new NullReferenceException($"UserDescriptMapper failed: {User}");
        var versionBypass = false;

        if (descriptor.Id is not null)
        {
            var user = await userService.GetUserByIdAsync(descriptor.Id.Value) ?? throw new NullReferenceException($"GetUserByIdAsync() returned null: {descriptor.Id.Value}");
            descriptor = descriptMapper.Map(user, descriptor.AccessToken!, descriptor.IdentityToken!);
            versionBypass = true;
        }

        return Ok(tokenIssuer.Issue(descriptor, versionBypass));
    }
}
