using API.Services.Interfaces;
using API.Utilities;
using API.Utilities.Interfaces;
using API.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
public class RemoveUserController : ControllerBase
{
    [Authorize(Roles = RoleKey.Admin)]
    [HttpDelete]
    [Route("user/remove/{userToBeDeletedId:guid}")]
    public async Task<IActionResult> RemoveUser(Guid userToBeDeletedId, IUserDescriptorMapper mapper, IUserService userService, ILogger<RemoveUserController> logger)
    {
        var descriptor = mapper.Map(User) ?? throw new NullReferenceException($"UserDescriptorMapper failed: {User}");
        if (userToBeDeletedId == descriptor.Id)
        {
            return BadRequest("A user cannot delete themselves.");
        }
        var user = await userService.GetUserByIdAsync(userToBeDeletedId);
        if (user is null) return NotFound("The user to be deleted was not found.");

        var deleted = await userService.RemoveUserAsync(user);
        if (deleted == false) return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the user.");

        logger.AuditLog(
            "User: {userId} was removed by {User} at {TimeStamp}.",
            userToBeDeletedId,
            user.Id,
            DateTimeOffset.Now.ToUnixTimeSeconds()
        );
        return Ok();
    }
}
