using System;
using System.Linq;
using API.ApiModels.Responses;
using API.Data;
using API.Exceptions;
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
        var subject = new Guid(User.FindSubjectGuidClaim());

        var connections = connectionRepository.GetCompanyConnections(subject);

        if (!connections.Any())
        {
            return NoContent();
        }

        var dtos = connections.Select(x => ToDto(x, subject)).ToList();

        return Ok(new ConnectionsResponse(dtos));
    }

    private static ConnectionDto ToDto(Connection connection, Guid loggedInCompanyId)
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
