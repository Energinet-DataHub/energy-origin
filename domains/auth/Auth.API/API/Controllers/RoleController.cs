using API.Models.Entities;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.AuthorizePolicies;
using API.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
public class RoleController : ControllerBase
{
    [Authorize(Policy = nameof(RoleAdminPolicy))]
    [HttpPut]
    [Route("role/assign")]
    public async Task<IActionResult> AssignRole([FromBody] RoleRequest roleRequest, IRoleService roleService, IUserService userService, ILogger<RoleController> logger, IUserDescriptorMapper mapper)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        var role = await roleService.GetRollByKeyAsync(roleRequest.RoleKey) ?? throw new NullReferenceException($"Role not found: {roleRequest.RoleKey}");
        var user = await userService.GetUserByIdAsync(roleRequest.UserId) ?? throw new NullReferenceException($"User not found: {roleRequest.UserId}");

        user.UserRoles.Add(new UserRole { RoleId = (Guid)role.Id!, UserId = roleRequest.UserId });
        await userService.UpsertUserAsync(user);
        logger.AuditLog(
            "{Role} was assign to {User} by {AdminId} at {TimeStamp}.",
            role.Name,
            user.Id,
            descriptor.Id,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );
        return Ok();
    }

    [Authorize(Policy = nameof(RoleAdminPolicy))]
    [HttpPut]
    [Route("role/remove")]
    public async Task<IActionResult> RemoveRoleFromUser([FromBody] RoleRequest roleRequest, IUserService userService, ILogger<RoleController> logger, IUserDescriptorMapper mapper)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        var user = await userService.GetUserByIdAsync(roleRequest.UserId) ?? throw new NullReferenceException($"User not found: {roleRequest.UserId}");
        var userRole = user.UserRoles.FirstOrDefault(x => x.Role.Key == roleRequest.RoleKey) ?? throw new NullReferenceException($"Remove role failed: {User}");
        if (user.Id == descriptor.Id)
        {
            return BadRequest("An admin cannot remove his admin role");
        }
        user.UserRoles.Remove(userRole);
        await userService.UpsertUserAsync(user);
        logger.AuditLog(
            "{Role} was removed from {User} by {AdminId} at {TimeStamp}. ",
            roleRequest.RoleKey,
            user.Id,
            descriptor.Id,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );
        return Ok();
    }
}
