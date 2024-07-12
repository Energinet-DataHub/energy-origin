using System.Threading.Tasks;
using API.Authorization._Features_;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Authorization.Controllers;

[ApiController]
[Authorize(Policy.B2CCvrClaim)]
[ApiVersion(ApiVersions.Version20230101)]
public class TermsController(IMediator mediator, IdentityDescriptor identityDescriptor) : ControllerBase
{
    [HttpPost]
    [Route("api/authorization/terms/accept")]
    [ProducesResponseType(typeof(AcceptTermsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AcceptTermsResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AcceptTermsResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AcceptTermsResponseDto>> AcceptTerms()
    {
        var command = new AcceptTermsCommand(identityDescriptor.OrganizationCvr!, identityDescriptor.OrganizationName, identityDescriptor.Subject);
        var result = await mediator.Send(command);

        if (!result)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new AcceptTermsResponseDto(false, "An unexpected error occurred while processing the request."));
        }

        return Ok(new AcceptTermsResponseDto(true, "Terms accepted successfully."));
    }
}

public record AcceptTermsResponseDto(bool Status, string Message);