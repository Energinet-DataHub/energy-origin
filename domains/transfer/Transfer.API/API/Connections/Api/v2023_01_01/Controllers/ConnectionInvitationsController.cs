using System;
using System.Threading.Tasks;
using API.Connections.Api.Models;
using API.Connections.Api.Repository;
using API.Shared.Extensions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Connections.Api.v2023_01_01.Controllers;

[Authorize]
[ApiController]
[ApiVersion("20230101", Deprecated = true)]
[Route("api/connection-invitations")]
public class ConnectionInvitationsController : ControllerBase
{
    private readonly IConnectionInvitationRepository connectionInvitationRepository;
    private readonly IConnectionRepository connectionRepository;

    public ConnectionInvitationsController(
        IConnectionInvitationRepository connectionInvitationRepository,
        IConnectionRepository connectionRepository)
    {
        this.connectionInvitationRepository = connectionInvitationRepository;
        this.connectionRepository = connectionRepository;
    }

    [ProducesResponseType(typeof(Guid), 201)]
    [HttpPost]
    public async Task<ActionResult> CreateConnectionInvitation()
    {
        var companySenderId = Guid.Parse(User.FindSubjectGuidClaim());
        var companySenderTin = User.FindSubjectTinClaim();

        var newInvitation = new ConnectionInvitation
        {
            SenderCompanyId = companySenderId,
            SenderCompanyTin = companySenderTin
        };

        await connectionInvitationRepository.AddConnectionInvitation(newInvitation);

        return CreatedAtAction(nameof(GetConnectionInvitation), new { id = newInvitation.Id }, newInvitation);
    }

    /// <summary>
    /// Get connection-invitation by Id
    /// </summary>
    /// <param name="id">Id of connection-invitation</param>
    /// <response code="200">Successful operation</response>
    /// <response code="400">You cannot Accept/Deny your own ConnectionInvitation</response>
    /// <response code="404">Connection-invitation expired or deleted</response>
    /// <response code="409">Company is already a connection</response>
    [ProducesResponseType(typeof(ConnectionInvitation), 200)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(string), 409)]
    [HttpGet("{id}")]
    public async Task<ActionResult<ConnectionInvitation>> GetConnectionInvitation(Guid id)
    {
        var connectionInvitation = await connectionInvitationRepository.GetNonExpiredConnectionInvitation(id);

        if (connectionInvitation == null)
        {
            return NotFound("Connection-invitation expired or deleted");
        }

        var currentCompanyId = Guid.Parse(User.FindSubjectGuidClaim());

        if (currentCompanyId == connectionInvitation.SenderCompanyId)
        {
            return BadRequest("You cannot Accept/Deny your own ConnectionInvitation");
        }

        var hasConflict = await connectionRepository.HasConflict(currentCompanyId, connectionInvitation.SenderCompanyId);

        if (hasConflict)
        {
            return Conflict("Company is already a connection");
        }

        return Ok(connectionInvitation);
    }

    /// <summary>
    /// Delete connection-invitation
    /// </summary>
    /// <param name="id">Id of connection-invitation</param>
    /// <response code="204">Successful operation</response>
    /// <response code="404">Connection-invitation not found</response>
    [ProducesResponseType(typeof(void), 204)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteConnectionInvitation(Guid id)
    {
        var connectionInvitation = await connectionInvitationRepository.GetNonExpiredConnectionInvitation(id);

        if (connectionInvitation == null)
        {
            return NotFound("Connection-invitation not found");
        }

        await connectionInvitationRepository.DeleteConnectionInvitation(id);

        return NoContent();
    }
}
