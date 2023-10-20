using System;
using System.Linq;
using System.Threading.Tasks;
using API.Connections.Api.Dto.Requests;
using API.Connections.Api.Dto.Responses;
using API.Connections.Api.Exceptions;
using API.Connections.Api.Repository;
using API.Shared.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Connections.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/connections")]
public class ConnectionsController : Controller
{
    private readonly IConnectionRepository connectionRepository;
    private readonly IConnectionInvitationRepository connectionInvitationRepository;


    public ConnectionsController(IConnectionRepository connectionRepository, IConnectionInvitationRepository connectionInvitationRepository)
    {
        this.connectionRepository = connectionRepository;
        this.connectionInvitationRepository = connectionInvitationRepository;
    }

    /// <summary>
    /// Add a new connection
    /// </summary>
    /// <param name="request">The request object containing the ConnectionInvitationId for creating the connection.</param>
    /// <response code="201">Successful operation</response>
    /// <response code="404">Connection-invitation expired or deleted</response>
    /// <response code="409">Company is already a connection</response>
    [HttpPost]
    [ProducesResponseType(typeof(Models.Connection), 201)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<ActionResult> Create([FromBody] CreateConnection request)
    {
        var connectionInvitation = await connectionInvitationRepository.GetNonExpiredConnectionInvitation(request.ConnectionInvitationId);
        if (connectionInvitation == null)
        {
            return NotFound("Connection-invitation expired or deleted");
        }

        var companyBId = new Guid(User.FindSubjectGuidClaim());
        var companyBTin = User.FindSubjectTinClaim();

        var connection = new Models.Connection
        {
            Id = Guid.NewGuid(),
            CompanyAId = connectionInvitation.SenderCompanyId,
            CompanyATin = connectionInvitation.SenderCompanyTin,
            CompanyBId = companyBId,
            CompanyBTin = companyBTin
        };

        var hasConflict = await connectionRepository.HasConflict(connection.CompanyAId, connection.CompanyBId);
        if (hasConflict)
        {
            return Conflict("Company is already a connection");
        }

        await connectionRepository.AddConnectionAndDeleteInvitation(connection, request.ConnectionInvitationId);

        return CreatedAtAction(nameof(GetConnections), new { id = connection.Id }, connection);
    }

    [ProducesResponseType(typeof(ConnectionsResponse), 200)]
    [ProducesResponseType(204)]
    [HttpGet]
    public async Task<ActionResult<ConnectionsResponse>> GetConnections()
    {
        var subject = new Guid(User.FindSubjectGuidClaim());

        var connections = await connectionRepository.GetCompanyConnections(subject);

        if (!connections.Any<Models.Connection>())
        {
            return NoContent();
        }

        var dtos = connections.Select<Models.Connection, ConnectionDto>(x => ToDto(x, subject)).ToList();

        return Ok(new ConnectionsResponse { Result = dtos });
    }

    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var subject = new Guid(User.FindSubjectGuidClaim());

        var connection = await connectionRepository.GetConnection(id);

        if (connection == null || (subject != connection.CompanyAId && subject != connection.CompanyBId))
        {
            return NotFound();
        }

        await connectionRepository.DeleteConnection(id);

        return NoContent();
    }

    private static ConnectionDto ToDto(Models.Connection connection, Guid loggedInCompanyId)
    {
        if (loggedInCompanyId == connection.CompanyAId)
            return new ConnectionDto
            {
                Id = connection.Id,
                CompanyId = connection.CompanyBId,
                CompanyTin = connection.CompanyBTin
            };

        if (loggedInCompanyId == connection.CompanyBId)
            return new ConnectionDto
            {
                Id = connection.Id,
                CompanyId = connection.CompanyAId,
                CompanyTin = connection.CompanyATin
            };

        throw new MappingException($"Connection is not owned by the user. Connection: {connection}, logged in companyId: {loggedInCompanyId}");
    }
}
