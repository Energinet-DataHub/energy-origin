using System.Threading.Tasks;
using API.Authorization._Features_;
using API.Authorization._Features_.Internal;
using Asp.Versioning;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Authorization.Controllers;

[ApiController]

[ApiVersion(ApiVersions.Version1)]

public class ServiceProviderTermsController(IMediator mediator, IdentityDescriptor identityDescriptor) : ControllerBase
{
    [HttpPost]
    [Route("api/authorization/service-provider-terms")]
    [Authorize(Policy = Policy.Frontend)]
    [ProducesResponseType(typeof(AcceptServiceProviderTermsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AcceptServiceProviderTermsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AcceptServiceProviderTermsResponse), StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Accept Service Provider Terms on behalf of users affiliated organization",
        Description = "Requires 'org_id' claim"
    )]
    public async Task<ActionResult<AcceptServiceProviderTermsResponse>> AcceptServiceProviderTerms()
    {
        var command = new AcceptServiceProviderTermsCommand(OrganizationId.Create(identityDescriptor.OrganizationId));
        await mediator.Send(command);
        return Ok(new AcceptServiceProviderTermsResponse("Service Provider Terms accepted successfully."));
    }

    [HttpGet]
    [Route("api/authorization/service-provider-terms")]
    [Authorize(Policy = Policy.Frontend)]
    [ProducesResponseType(typeof(GetServiceProviderTermsResponse), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Find Service Provider Terms Status on behalf of users affiliated organization",
        Description = "Requires 'org_id' claim"
    )]
    public async Task<ActionResult<GetServiceProviderTermsResponse>> GetServiceProviderTerms()
    {
        var query = new GetServiceProviderTermsForOrganizationQuery(OrganizationId.Create(identityDescriptor.OrganizationId));

        var termsAccepted = await mediator.Send(query);

        return Ok(new GetServiceProviderTermsResponse(termsAccepted));
    }
}


