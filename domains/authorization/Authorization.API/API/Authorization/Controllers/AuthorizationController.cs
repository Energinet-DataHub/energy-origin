using System;
using System.Threading.Tasks;
using API.Authorization._Features_;
using API.Models;
using API.ValueObjects;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Authorization.Controllers;

[ApiController]
[ApiVersionNeutral] // B2C does not support adding a version header
[Authorize(Policy = Policy.B2CCustomPolicyClientPolicy)]
public class AuthorizationController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Retrieves Authorization Model.
    /// </summary>
    [HttpPost]
    [Route("api/authorization/client-consent/")]
    public async Task<ActionResult<AuthorizationResponse>> GetConsentForClient(
        [FromServices] ILogger<AuthorizationController> logger, [FromBody] AuthorizationClientRequest request)
    {
        var queryResult = await mediator.Send(new GetConsentForClientQuery(request.ClientId));

        return Ok(new AuthorizationResponse(queryResult.Sub, queryResult.SubType, queryResult.OrgName,
            queryResult.OrgIds, queryResult.Scope));
    }

    /// <summary>
    /// Retreives Authorization Model.
    /// </summary>
    [HttpPost]
    [Route("api/authorization/user-consent/")]
    public async Task<ActionResult<UserAuthorizationResponse>> GetConsentForUser(
        [FromServices] ILogger<AuthorizationController> logger,
        [FromBody] AuthorizationUserRequest request)
    {
        var queryResult = await mediator.Send(new GetConsentForUserQuery(request.Sub, request.Name, request.OrgName, request.OrgCvr));

        return Ok(new UserAuthorizationResponse(
            queryResult.Sub,
            queryResult.SubType,
            queryResult.OrgName,
            queryResult.OrgIds,
            queryResult.Scope,
            queryResult.TermsAccepted));
    }
}
