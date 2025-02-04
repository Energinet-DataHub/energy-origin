using System;
using System.Threading.Tasks;
using API.Authorization._Features_;
using API.ValueObjects;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Authorization.Controllers;

[ApiController]
[Authorize(Policy = Policy.Frontend)]
[ApiVersion(ApiVersions.Version1)]
public class OrganizationController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("api/authorization/organization/{organizationId:guid}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [SwaggerOperation(
        Summary = "Retrieves Organization",
        Description = "Retrieves info for organization with id organizationId"
    )]
    public async Task<ActionResult<ClientResponse>> GetClient([FromServices] ILogger<ClientResponse> logger, [FromRoute] Guid organizationId)
    {
        var queryResult = await _mediator.Send(new GetOrganizationQuery(new OrganizationId(organizationId)));
        return Ok(new OrganizationResponse(queryResult.OrganizationId.Value, queryResult.OrganizationName.Value, queryResult.Tin?.Value));
    }
}
