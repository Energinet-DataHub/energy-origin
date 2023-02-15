using API.DTOs;
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
    public async Task<IActionResult> AcceptTermsAsync([FromBody] AcceptTermsDTO acceptedTermsVersion, [FromServices] IUserDescriptMapper descriptMapper, [FromServices] IUserService userService)
    {
        var descriptor = descriptMapper.Map(User) ?? throw new NullReferenceException($"UserDescriptMapper failed: {User}");

        User user;

        if (descriptor.Id is null)
        {
            user = new User()
            {
                Name = descriptor.Name,
                ProviderId = descriptor.ProviderId,
                Tin = descriptor.Tin,
                AllowCPRLookup = descriptor.AllowCPRLookup
            };
        }
        else
        {
            user = await userService.GetUserByIdAsync((Guid)descriptor.Id) ?? throw new NullReferenceException($"GetUserByIdAsync() returned null: {descriptor.Id}");
        }

        user.AcceptedTermsVersion = acceptedTermsVersion.Version;

        await userService.UpsertUserAsync(user);

        return NoContent();
    }
}
