using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using API.Authorization._Features_;
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

        return Ok(new AuthorizationResponse(queryResult.Sub, queryResult.Name, queryResult.SubType, queryResult.OrgName, queryResult.OrgIds, queryResult.Scope));
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

        return Ok(new AuthorizationResponse(queryResult.Sub, queryResult.Name, queryResult.SubType, queryResult.OrgName, queryResult.OrgIds, queryResult.Scope));
    }
}

public static class ApiVersions
{
    public const string Version20230101 = "20230101";
}
