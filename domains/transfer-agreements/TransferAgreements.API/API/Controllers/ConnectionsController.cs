using System;
using System.Linq;
using API.ApiModels.Responses;
using API.Data;
using API.Extensions;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/connections")]
public class ConnectionsController : Controller
{
    private readonly IConnectionRepository connectionRepository;

    public ConnectionsController(IConnectionRepository connectionRepository)
    {
        this.connectionRepository = connectionRepository;
    }

    [ProducesResponseType(typeof(ConnectionsResponse), 200)]
    [ProducesResponseType(204)]
    [HttpGet]
    public ActionResult<ConnectionsResponse> GetConnections()
    {
        var subject = User.FindSubjectGuidClaim();

        var connections = connectionRepository.GetOwnedConnections(new Guid(subject));

        if (!connections.Any())
        {
            return NoContent();
        }

        var dtos = connections.Select(ToDto).ToList();

        return Ok(new ConnectionsResponse(dtos));
    }

    private static ConnectionDto ToDto(Connection connection) =>
        new(connection.OrganizationId, connection.OrganizationTin);
}
