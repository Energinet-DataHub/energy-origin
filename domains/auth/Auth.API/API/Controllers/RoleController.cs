using API.Models.Entities;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.AuthorizePolicies;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
public class RoleController : ControllerBase
{
    [Authorize(Policy = nameof(RoleAdminPolicy))]
    [HttpPut]
    [Route("role/assignRole")]
    public async Task<IActionResult> AssignRole([FromBody] RoleRequest roleRequest, IRoleService roleService, IUserService userService, ILogger<RoleController> logger, IUserDescriptorMapper mapper)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        var role = await roleService.GetRollByKeyAsync(roleRequest.RoleKey);
        var user = await userService.GetUserByIdAsync(roleRequest.UserId);
        if (role?.Id is not null && user is not null)
        {
            user.UserRoles.Add(new UserRole { RoleId = (Guid)role.Id, UserId = roleRequest.UserId});
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
        throw new NullReferenceException($"Assign role failed: {User}");
    }

    [Authorize(Policy = nameof(RoleAdminPolicy))]
    [HttpPut]
    [Route("role/removeRoleFromUser")]
    public async Task<IActionResult> RemoveRoleFromUser([FromBody] RoleRequest roleRequest, IRoleService roleService, IUserService userService, ILogger<RoleController> logger, IUserDescriptorMapper mapper)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");

        var user = await userService.GetUserByIdAsync(roleRequest.UserId);
        var userRole = user?.UserRoles.FirstOrDefault(x => x.Role.Key == roleRequest.RoleKey);
        if (userRole is not null)
        {
            user!.UserRoles.Remove(userRole);
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
        throw new NullReferenceException($"Remove role failed: {User}");
    }
}
