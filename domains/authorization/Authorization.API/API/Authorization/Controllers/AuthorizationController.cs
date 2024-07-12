using System.Threading.Tasks;
using API.Authorization._Features_;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Authorization.Controllers;

[ApiController]
[Authorize(Policy = Policy.B2CCustomPolicyClientPolicy)] // B2C does not support adding a version header
[Route("api/authorization/api/authorization")]
[ApiVersionNeutral]
public class AuthorizationController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Route("client-consent/")]
    [SwaggerOperation(
        Summary = "Retrieves Client Authorization Model",
        Description = "This endpoint is only used by Azure B2C",
        OperationId = "RetrieveClientInfo",
        Tags = ["Retrieve", "ClientInfo"]
    )]
    public async Task<ActionResult<AuthorizationResponse>> GetClientInfo(
        [FromServices] ILogger<AuthorizationController> logger, [FromBody] AuthorizationClientRequest request)
    {
        var queryResult = await mediator.Send(new GetConsentForClientQuery(request.ClientId));

        return Ok(new AuthorizationResponse(
            queryResult.Sub,
            queryResult.SubType,
            queryResult.OrgName,
            queryResult.OrgIds,
            queryResult.Scope)
        );
    }

    [HttpPost]
    [Route("user-consent/")]
    [SwaggerOperation(
        Summary = "Retrieves User Authorization Model",
        Description = "This endpoint is only used by Azure B2C",
        OperationId = "RetrieveUserInfo",
        Tags = ["Retrieve", "UserInfo"]
    )]
    public async Task<ActionResult<UserAuthorizationResponse>> GetUserInfo(
        [FromServices] ILogger<AuthorizationController> logger, [FromBody] AuthorizationUserRequest request)
    {
        var commandResult = await mediator.Send(new GetConsentForUserCommand(
            request.Sub,
            request.Name,
            request.OrgName,
            request.OrgCvr));

        return Ok(new UserAuthorizationResponse(
            commandResult.Sub,
            commandResult.SubType,
            commandResult.OrgName,
            commandResult.OrgIds,
            commandResult.Scope,
            commandResult.TermsAccepted));
    }
}
