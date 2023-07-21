using API.Utilities.AuthorizePolicies;
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
    public async Task<IActionResult> AssignRole([FromBody] RoleRequest roleRequest)
    {

    }

    [Authorize(Policy = nameof(RoleAdminPolicy))]
    [HttpPut]
    [Route("role/removeRoleFromUser")]
    public async Task<IActionResult> RemoveRoleFromUser([FromBody] RoleRequest roleRequest)
    {

    }
}
