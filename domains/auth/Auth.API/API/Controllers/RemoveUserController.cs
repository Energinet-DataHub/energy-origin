using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.AuthorizePolicies;
using API.Utilities.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
public class RemoveUserController: ControllerBase
{
    [Authorize(Policy = nameof(RoleAdminPolicy))]
    [HttpDelete]
    [Route("user/remove")]
    public async Task<IActionResult> RemoveUser([FromBody] Guid userToBeDeletedId, IUserDescriptorMapper mapper, IUserService userService,  ILogger<RemoveUserController> logger)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        if (userToBeDeletedId == descriptor.Id)
        {
            return BadRequest("A user cannot delete themselves.");
        }
        var user = await userService.GetUserByIdAsync(userToBeDeletedId);
        if (user is null) return NotFound("The user to be deleted was not found.");

        var deleted = await userService.RemoveUserAsync(user);
        if (!deleted) return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the user.");

        logger.AuditLog(
            "User: {userId} was removed by {User} at {TimeStamp}.",
            userToBeDeletedId,
            user.Id,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );
        return new NoContentResult();
    }
}
