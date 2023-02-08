using System.Security.Claims;
using API.Models;
using API.Options;
using API.Services;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TermsController : ControllerBase
{
    [HttpPut()]
    [Route("terms/accept")]
    public async Task<ActionResult<User>> AcceptTermsAsync([FromBody] int acceptedTermsVersion, [FromServices] IUserService userService)
    {
        var id = User.FindFirstValue("sub") ?? Guid.Empty.ToString();

        var user = await userService.GetUserByIdAsync(Guid.Parse(id)) ?? new User()
        {
            Name = User.Claims.FirstOrDefault(x => x.Type == "name")!.Value,
            ProviderId = User.Claims.FirstOrDefault(x => x.Type == "providerId")!.Value,
            Tin = User.Claims.FirstOrDefault(x => x.Type == "tin")?.Value,
            AllowCPRLookup = bool.Parse(User.Claims.FirstOrDefault(x => x.Type == "allowCprLookup")!.Value)
        };
        user.AcceptedTermsVersion = acceptedTermsVersion;

        return Ok(
            await userService.UpsertUserAsync(user)
        );
    }
}
