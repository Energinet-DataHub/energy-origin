using API.Models.Entities;
using API.Services;
using API.Utilities;
using EnergyOrigin.TokenValidation.Models.Requests;
using EnergyOrigin.TokenValidation.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TermsController : ControllerBase
{
    [HttpPut()]
    [Route("terms/accept")]
    public async Task<IActionResult> AcceptTermsAsync(
        [FromServices] IUserDescriptMapper descriptMapper,
        [FromServices] IUserService userService,
        [FromBody] AcceptTermsRequest acceptedTermsVersion)
    {
        var descriptor = descriptMapper.Map(User) ?? throw new NullReferenceException($"UserDescriptMapper failed: {User}");

        User user;
        if (descriptor.Id is null)
        {
            user = new User
            {
                Name = descriptor.Name,
                ProviderId = descriptor.ProviderId,
                Tin = descriptor.Tin,
                AllowCPRLookup = descriptor.AllowCPRLookup
            };
        }
        else
        {
            var id = descriptor.Id.Value;
            user = await userService.GetUserByIdAsync(id) ?? throw new NullReferenceException($"GetUserByIdAsync() returned null: {id}");
        }

        user.AcceptedTermsVersion = acceptedTermsVersion.Version;

        await userService.UpsertUserAsync(user);

        return NoContent();
    }
}
