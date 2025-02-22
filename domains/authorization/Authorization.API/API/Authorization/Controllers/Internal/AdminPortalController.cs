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
[ApiVersionNeutral]
[Route("api/authorization/admin-portal")]
[ApiExplorerSettings(IgnoreApi = true)]
public class AdminPortalController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policy.AdminPortal)]
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
    [Authorize(Policy = Policy.WorkloadIdentity)]
    [Route("first-party-organizations-workload/")]
    [ProducesResponseType(typeof(FirstPartyOrganizationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FirstPartyOrganizationsResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFirstPartyOrganizationsWorkload(
        [FromServices] ILogger<AdminPortalController> logger, CancellationToken cancellationToken)
    {
        var queryResult = await mediator.Send(new GetFirstPartyOrganizationsQuery(), cancellationToken);

        var responseItems = queryResult.Result
            .Select(o => new FirstPartyOrganizationsResponseItem(o.OrganizationId, o.OrganizationName, o.Tin)).ToList();

        return Ok(new FirstPartyOrganizationsResponse(responseItems));
    }
}
