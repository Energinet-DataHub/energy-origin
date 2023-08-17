using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize(Roles = RoleKey.UserAdmin, Policy = PolicyName.RequiresCompany)]
public class RemoveUserController : ControllerBase
{
    [HttpDelete]
    [Route("user/remove/{userId:guid}")]
    public async Task<IActionResult> RemoveUser(Guid userId, IUserDescriptorMapper mapper, IUserService userService, ILogger<RemoveUserController> logger)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        if (userId == descriptor.Id)
        {
            return BadRequest("A user cannot delete themselves.");
        }
        var user = await userService.GetUserByIdAsync(userId);
        if (user is null) return Ok();

        await userService.RemoveUserAsync(user);

        logger.AuditLog(
            "User: {userId} was removed by {User} at {TimeStamp}.",
            userId,
            user.Id,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );
        return Ok();
    }
}
