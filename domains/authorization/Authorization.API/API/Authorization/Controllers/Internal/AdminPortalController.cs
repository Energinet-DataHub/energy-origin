using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization._Features_.Internal;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
    public async Task<IActionResult> GetFirstPartyOrganizations(
        [FromServices] ILogger<AdminPortalController> logger, CancellationToken cancellationToken)
    {
        var queryResult = await mediator.Send(new GetFirstPartyOrganizationsQuery(), cancellationToken);

        var responseItems = queryResult.Result
            .Select(o => new FirstPartyOrganizationsResponseItem(o.OrganizationId, o.OrganizationName, o.Tin)).ToList();

        return Ok(new FirstPartyOrganizationsResponse(responseItems));
    }

    [HttpGet]
    [Route("whitelisted-organizations/")]
    [ProducesResponseType(typeof(WhitelistedOrganizationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(WhitelistedOrganizationsResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWhitelistedOrganizations(
        [FromServices] ILogger<AdminPortalController> logger, CancellationToken cancellationToken)
    {
        var queryResult = await mediator.Send(new GetWhitelistedOrganizationsQuery(), cancellationToken);

        var responseItems = queryResult.Result
            .Select(o => new WhitelistedOrganizationsResponseItem(o.OrganizationId, o.Tin))
            .ToList();

        return Ok(new WhitelistedOrganizationsResponse(responseItems));
    }
}
