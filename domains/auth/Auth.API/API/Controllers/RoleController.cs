using System.Collections.Generic;
using API.Models.Entities;
using API.Options;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using API.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
public class RoleController : ControllerBase
{
    [Authorize(Roles = RoleKey.Admin)]
    [HttpGet]
    [Route("role/all")]
    public IActionResult List(RoleOptions roles) => Ok(roles.RoleConfigurations.Where(x => !x.IsTransient).Select(x => new
    {
        x.Key,
        x.Name
    }));

    [Authorize(Roles = RoleKey.Admin)]
    [HttpPut]
    [Route("role/{role}/assign/{userId}")]
    public async Task<IActionResult> AssignRole(string role, Guid userId, RoleOptions roles, IUserService userService, ILogger<RoleController> logger, IUserDescriptorMapper mapper)
    {
        var validRoles = roles.RoleConfigurations.Select(x => x.Key);
        if (validRoles.Any(x => x == role) == false)
        {
            return BadRequest($"Role not found: {role}");
        }

        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");

        var user = await userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound($"User not found: {userId}");
        }

        if (user.UserRoles.Any(x => x.Role == role))
        {
            return Ok();
        }

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

    [Authorize(Roles = RoleKey.Admin)]
    [HttpPut]
    [Route("role/{role}/remove/{userId}")]
    public async Task<IActionResult> RemoveRoleFromUser(string role, Guid userId, IUserService userService, ILogger<RoleController> logger, IUserDescriptorMapper mapper)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");

        var user = await userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound($"User not found: {userId}");
        }

        if (user.Id == descriptor.Id)
        {
            return BadRequest("An admin cannot remove his admin role");
        }

        var userRole = user.UserRoles.SingleOrDefault(x => x.Role == role);
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
