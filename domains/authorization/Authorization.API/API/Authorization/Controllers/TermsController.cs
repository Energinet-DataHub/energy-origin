using System.Threading.Tasks;
using API.Authorization._Features_;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Authorization.Controllers;

[ApiController]

[ApiVersion(ApiVersions.Version1)]
[ApiVersion(ApiVersions.Version20230101, Deprecated = true)]
[ApiExplorerSettings(IgnoreApi = true)]
public class TermsController(IMediator mediator, IdentityDescriptor identityDescriptor) : ControllerBase
{
    [HttpPost]
    [Route("api/authorization/terms/accept")]
    [Authorize(Policy = Policy.FrontendWithoutTermsAccepted)]
    [ProducesResponseType(typeof(AcceptTermsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AcceptTermsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AcceptTermsResponse), StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Accept Terms on behalf of users affiliated organization",
        Description = "Requires 'org_cvr' claim"
    )]
    public async Task<ActionResult<AcceptTermsResponse>> AcceptTerms()
    {
        var command = new AcceptTermsCommand(identityDescriptor.OrganizationCvr!, identityDescriptor.OrganizationName, identityDescriptor.Subject);
        await mediator.Send(command);
        return Ok(new AcceptTermsResponse("Terms accepted successfully."));
    }

    [HttpPost]
    [Route("api/authorization/terms/revoke")]
    [Authorize(Policy = Policy.Frontend)]
    [ProducesResponseType(typeof(RevokeTermsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RevokeTermsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RevokeTermsResponse), StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Revoke Terms on behalf of users affiliated organization",
        Description = "Requires 'org_id' claim"
    )]
    public async Task<ActionResult<AcceptTermsResponse>> RevokeTerms()
    {
        await mediator.Send(new RevokeTermsCommand(identityDescriptor.OrganizationId));
        return Ok(new RevokeTermsResponse("Terms revoked successfully."));
    }
}

public record AcceptTermsResponse(string Message);
public record RevokeTermsResponse(string Message);
