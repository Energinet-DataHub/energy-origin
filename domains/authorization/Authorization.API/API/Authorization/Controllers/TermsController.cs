using System;
using System.Threading.Tasks;
using API.Authorization._Features_;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Authorization.Controllers;

[ApiController]
public class TermsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Route("api/authorization/terms/accept")]
    [Authorize(Policy = Policy.B2CPolicy)]
    [ProducesResponseType(typeof(AcceptTermsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AcceptTermsResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AcceptTermsResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AcceptTermsResponseDto>> AcceptTerms([FromBody] AcceptTermsRequest request)
    {
        var command = new AcceptTermsCommand(request.OrgCvr, request.UserId, request.UserName);
        var result = await mediator.Send(command);

        if (result)
        {
            return Ok(new AcceptTermsResponseDto(true, "Terms accepted successfully."));
        }

        return BadRequest(new AcceptTermsResponseDto(false, "Failed to accept terms."));
    }
}

public record AcceptTermsRequest(string OrgCvr, Guid UserId, string UserName);
public record AcceptTermsResponseDto(bool Status, string Message);


