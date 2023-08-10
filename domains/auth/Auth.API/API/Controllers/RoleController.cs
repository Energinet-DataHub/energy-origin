using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
public class RoleController : ControllerBase
{
    [Authorize(Roles = "Administrator")]
    [HttpPut]
    [Route("role/{role}/assign/{userId}")]
    public async Task<IActionResult> AssignRole([FromRoute] string role, [FromRoute] Guid userId, IOptions<RoleOptions> roles, IUserService userService, ILogger<RoleController> logger, IUserDescriptorMapper mapper)
    {
        var validRoles = roles.Value.RoleConfigurations.Select(x => x.Key);
        _ = validRoles.First(x => x == role) ?? throw new NullReferenceException($"Role not found: {role}");

        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        var user = await userService.GetUserByIdAsync(userId) ?? throw new NullReferenceException($"User not found: {userId}");

        user.UserRoles.Add(new UserRole { Role = role, UserId = userId });
        await userService.UpsertUserAsync(user);
        logger.AuditLog(
            "{Role} was assign to {User} by {AdminId} at {TimeStamp}.",
            role,
            user.Id,
            descriptor.Id,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );
        return Ok();
    }

    [Authorize(Roles = "Administrator")]
    [HttpPut]
    [Route("role/{role}/remove/{userId}")]
    public async Task<IActionResult> RemoveRoleFromUser([FromRoute] string role, [FromRoute] Guid userId, IUserService userService, ILogger<RoleController> logger, IUserDescriptorMapper mapper)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        var user = await userService.GetUserByIdAsync(userId) ?? throw new NullReferenceException($"User not found: {userId}");
        if (user.Id == descriptor.Id)
        {
            return BadRequest("An admin cannot remove his admin role");
        }

        var userRole = user.UserRoles.FirstOrDefault(x => x.Role == role);
        if (userRole == null)
        {
            return Ok();
        }

        user.UserRoles.Remove(userRole);
        await userService.UpsertUserAsync(user);
        logger.AuditLog(
            "{Role} was removed from {User} by {AdminId} at {TimeStamp}. ",
            role,
            user.Id,
            descriptor.Id,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );
        return Ok();
    }
}
