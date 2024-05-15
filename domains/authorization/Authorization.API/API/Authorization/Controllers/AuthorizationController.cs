using System;
using System.Threading.Tasks;
using API.Authorization._Features_;
using API.ValueObjects;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Authorization.Controllers;

[ApiController]
[ApiVersion(ApiVersions.Version20230101)]
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
    [Authorize]
    [Route("api/authorization/client-consent/")]
    public async Task<ActionResult<AuthorizationResponse>> GetConsentForClient([FromServices] ILogger<AuthorizationController> logger, [FromBody] AuthorizationClientRequest request)
    {
        var queryResult = await _mediator.Send(new GetConsentForClientQuery(request.ClientId));

        return Ok(new AuthorizationResponse(queryResult.Sub, queryResult.SubType, queryResult.OrgName, queryResult.OrgIds, queryResult.Scope));
    }

    /// <summary>
    /// Retreives Authorization Model.
    /// </summary>
    [HttpPost]
    [Authorize]
    [Route("api/authorization/user-consent/")]
    public async Task<ActionResult<AuthorizationResponse>> GetConsentForUser([FromServices] ILogger<AuthorizationController> logger, [FromBody] AuthorizationUserRequest request)
    {
        var queryResult = await _mediator.Send(new GetConsentForUserQuery(request.Sub, request.Name, request.OrgName, request.OrgCvr));

        return Ok(new AuthorizationResponse(queryResult.Sub, queryResult.SubType, queryResult.OrgName, queryResult.OrgIds, queryResult.Scope));
    }

    /// <summary>
    /// Retreives Client.
    /// </summary>
    [HttpGet]
    [Authorize]
    [Route("api/authorization/client/{idpClientId}")]
    public async Task<ActionResult<AuthorizationResponse>> GetClient([FromServices] ILogger<AuthorizationController> logger, [FromRoute] Guid idpClientId)
    {
        var queryResult = await _mediator.Send(new GetlientQuery(new IdpClientId(idpClientId)));

        return Ok(new ClientResponse(queryResult.IdpClientId.Value, queryResult.Name.Value, queryResult.RedirectUrl));
    }
}

public static class ApiVersions
{
    public const string Version20230101 = "20230101";
}
