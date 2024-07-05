using System;
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
[ApiVersion(ApiVersions.Version20230101)]
public class TermsController(IMediator mediator, IdentityDescriptor identityDescriptor) : ControllerBase
{
    [HttpPost]
    [Route("api/authorization/terms/accept")]
    [Authorize(Policy.B2CCvrClaim)]
    [Authorize(Policy.B2CSubTypeUserPolicy)]
    [ProducesResponseType(typeof(AcceptTermsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AcceptTermsResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AcceptTermsResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AcceptTermsResponseDto>> AcceptTerms([FromBody] AcceptTermsRequest request)
    {
        var command = new AcceptTermsCommand(identityDescriptor.OrganizationCvr!, identityDescriptor.Subject, identityDescriptor.Name, identityDescriptor.OrganizationName);
        var result = await mediator.Send(command);

        if (!result)
        {
            return BadRequest(new AcceptTermsResponseDto(false, "Failed to accept terms."));
        }

        return Ok(new AcceptTermsResponseDto(true, "Terms accepted successfully."));
    }
}

public record AcceptTermsRequest(string OrgCvr, Guid UserId, string UserName, string OrganizationName);
public record AcceptTermsResponseDto(bool Status, string Message);


