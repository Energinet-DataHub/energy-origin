using System.Collections.Generic;
using System.Data;
using API.Models.Entities;
using API.Models.Response;
using API.Options;
using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using API.Values;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize(Roles = RoleKey.RoleAdmin, Policy = PolicyName.RequiresCompany)]
public class RoleController : ControllerBase
{
    [HttpGet]
    [Route("role/all")]
    public IActionResult List(RoleOptions roles) => Ok(roles.RoleConfigurations.Where(x => !x.IsTransient).Select(x => new
    {
        x.Key,
        x.Name
    }));

    [HttpPut]
    [Route("role/{role}/assign/{userId:guid}")]
    public async Task<IActionResult> AssignRole(string role, Guid userId, RoleOptions roles, IUserService userService, ILogger<RoleController> logger, IUserDescriptorMapper mapper)
    {
        var validRoles = roles.RoleConfigurations.Where(x => !x.IsTransient).Select(x => x.Key);
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
        if (user.Company?.Tin != descriptor.Tin)
        {
            return Forbid($"User is not in the same company");
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

    [HttpPut]
    [Route("role/{role}/remove/{userId:guid}")]
    public async Task<IActionResult> RemoveRoleFromUser(string role, Guid userId, IUserService userService, ILogger<RoleController> logger, IUserDescriptorMapper mapper)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");

        var user = await userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound($"User not found: {userId}");
        }
        if (user.Id == descriptor.Id && role == RoleKey.RoleAdmin)
        {
            return BadRequest("An admin cannot remove his admin role");
        }
        if (user.Company?.Tin != descriptor.Tin)
        {
            return Forbid($"User is not in the same company");
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

    [HttpGet]
    [Route("role/users")]
    public async Task<IActionResult> GetUsersByTin(IUserService userService, IUserDescriptorMapper mapper, RoleOptions roles, ILogger<RoleController> logger)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        var tin = descriptor.Tin!;

        var users = await userService.GetUsersByTinAsync(tin);

        var validRoles = roles.RoleConfigurations
            .Where(x => !x.IsTransient)
            .Select(x => (x.Key, x.Name))
            .ToList();
        
        var list = users.Select(user => new UserRolesResponse { UserId = user.Id!.Value, Name = user.Name, Roles = PopulateRoles(user.UserRoles, validRoles) }).ToList();

        logger.AuditLog(
           "List of {UserCount} users was retrieved from {tin} by {AdminId} at {TimeStamp}",
           list.Count,
           tin,
           descriptor.Id,
           DateTimeOffset.Now.ToUnixTimeSeconds()
       );
        return Ok(list);
    }

    private Dictionary<string,string> PopulateRoles(List<UserRole> userRoles, List<(string,string)> validRoles)
    {
        var roles = new Dictionary<string, string>();
        foreach (var role in userRoles)
        {
            var result = validRoles.FirstOrDefault(x => x.Item1 == role.Role);
            if (!EqualityComparer<(string, string)>.Default.Equals(result, default))
            {
                roles.Add(result.Item1, result.Item2);
            }
        }
        return roles;
    }
}
