using API.Models;
using API.Services;
using API.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TermsController : ControllerBase
{
    [HttpPut()]
    [Route("terms/accept")]
    public async Task<ActionResult<User>> AcceptTermsAsync([FromBody] int acceptedTermsVersion, [FromServices] IUserDescriptMapper descriptMapper, [FromServices] IUserService userService)
    {
        var descriptor = descriptMapper.Map(User);

        var user = await userService.GetUserByIdAsync(descriptor?.Id ?? Guid.Empty) ?? new User()
        {
            Name = descriptor!.Name,
            ProviderId = descriptor!.ProviderId,
            Tin = descriptor?.Tin,
            AllowCPRLookup = descriptor!.AllowCPRLookup
        };
        user.AcceptedTermsVersion = acceptedTermsVersion;

        return Ok(
            await userService.UpsertUserAsync(user)
        );
    }
}
