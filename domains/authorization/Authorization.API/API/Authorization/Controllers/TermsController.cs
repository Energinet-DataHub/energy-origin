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
[Authorize(Policy = Policy.FrontendWithoutTermsAccepted)]
[ApiVersion(ApiVersions.Version1)]
[ApiVersion(ApiVersions.Version20230101, Deprecated = true)]
[ApiExplorerSettings(IgnoreApi = true)]
public class TermsController(IMediator mediator, IdentityDescriptor identityDescriptor) : ControllerBase
{
    [HttpPost]
    [Route("api/authorization/terms/accept")]
    [ProducesResponseType(typeof(AcceptTermsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AcceptTermsResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AcceptTermsResponseDto), StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Accept Terms on behalf of users affiliated organization",
        Description = "Requires 'org_cvr' claim"
    )]
    public async Task<ActionResult<AcceptTermsResponseDto>> AcceptTerms()
    {
        var command = new AcceptTermsCommand(identityDescriptor.OrganizationCvr!, identityDescriptor.OrganizationName, identityDescriptor.Subject);
        await mediator.Send(command);
        return Ok(new AcceptTermsResponseDto("Terms accepted successfully."));
    }
}

public record AcceptTermsResponseDto(string Message);
