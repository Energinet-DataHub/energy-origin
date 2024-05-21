using System.Threading.Tasks;
using API.Authorization._Features_;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Authorization.Controllers;

[ApiController]
[ApiVersion(ApiVersions.Version20230101)]
[Authorize(Policy = Policy.B2CPolicy)]
public class AuthorizationController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthorizationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retreives Authorization Model.
    /// </summary>
    [HttpPost]
    [Route("api/authorization/client-consent/")]
    public async Task<ActionResult<AuthorizationResponse>> GetConsentForClient(
        [FromServices] ILogger<AuthorizationController> logger, [FromBody] AuthorizationClientRequest request)
    {
        var queryResult = await _mediator.Send(new GetConsentForClientQuery(request.ClientId));

        return Ok(new AuthorizationResponse(queryResult.Sub, queryResult.SubType, queryResult.OrgName,
            queryResult.OrgIds, queryResult.Scope));
    }

    /// <summary>
    /// Retreives Authorization Model.
    /// </summary>
    [HttpPost]
    [Route("api/authorization/user-consent/")]
    public async Task<ActionResult<AuthorizationResponse>> GetConsentForUser(
        [FromServices] ILogger<AuthorizationController> logger, [FromBody] AuthorizationUserRequest request)
    {
        var queryResult =
            await _mediator.Send(new GetConsentForUserQuery(request.Sub, request.Name, request.OrgName,
                request.OrgCvr));

        return Ok(new AuthorizationResponse(queryResult.Sub, queryResult.SubType, queryResult.OrgName,
            queryResult.OrgIds, queryResult.Scope));
    }
}
