using System;
using System.Threading.Tasks;
using API.Authorization._Features_;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Authorization.Controllers;

[ApiController]
public class TermsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Route("api/authorization/terms/accept")]
    [Authorize(Policy = Policy.B2CPolicy)]
    public async Task<IActionResult> AcceptTerms([FromBody] AcceptTermsRequest request)
    {
        var command = new AcceptTermsCommand(request.OrgCvr, request.UserId, request.UserName);
        var result = await mediator.Send(command);

        if (result)
        {
            return Ok(new { Message = "Terms accepted successfully." });
        }

        return BadRequest(new { Message = "Failed to accept terms." });
    }
}

public class AcceptTermsRequest
{
    public string OrgCvr { get; set; } = null!;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
}

