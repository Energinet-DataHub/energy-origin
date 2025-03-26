using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization._Features_.Internal;
using Asp.Versioning;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.Authorization.Controllers.Internal;

[ApiController]
[Authorize(Policy = Policy.AdminPortal)]
[ApiVersionNeutral]
[Route("api/authorization/admin-portal")]
[ApiExplorerSettings(IgnoreApi = true)]
public class AdminPortalController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Route("first-party-organizations/")]
    [ProducesResponseType(typeof(FirstPartyOrganizationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FirstPartyOrganizationsResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFirstPartyOrganizations(CancellationToken cancellationToken)
    {
        var queryResult = await mediator.Send(new GetFirstPartyOrganizationsQuery(), cancellationToken);

        var responseItems = queryResult.Result
            .Select(o => new FirstPartyOrganizationsResponseItem(o.OrganizationId, o.OrganizationName, o.Tin)).ToList();

        return Ok(new FirstPartyOrganizationsResponse(responseItems));
    }

    [HttpGet]
    [Route("whitelisted-organizations/")]
    [ProducesResponseType(typeof(GetWhitelistedOrganizationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GetWhitelistedOrganizationsResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWhitelistedOrganizations(CancellationToken cancellationToken)
    {
        var queryResult = await mediator.Send(new GetWhitelistedOrganizationsQuery(), cancellationToken);

        var responseItems = queryResult.Result
            .Select(o => new GetWhitelistedOrganizationsResponseItem(o.OrganizationId, o.Tin))
            .ToList();

        return Ok(new GetWhitelistedOrganizationsResponse(responseItems));
    }

    [HttpPost]
    [Route("whitelisted-organizations/")]
    [ProducesResponseType(typeof(AddOrganizationToWhitelistResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddOrganizationToWhitelist(
        [FromBody] AddOrganizationToWhitelistRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await mediator.Send(new AddOrganizationToWhitelistCommand(Tin.Create(request.Tin)), cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
        }

        return StatusCode(StatusCodes.Status201Created, new AddOrganizationToWhitelistResponse(request.Tin));
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
