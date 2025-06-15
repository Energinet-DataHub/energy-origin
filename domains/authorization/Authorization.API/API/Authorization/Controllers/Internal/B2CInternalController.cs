using System.Threading.Tasks;
using API.Authorization._Features_.Internal;
using API.Models;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Authorization.Controllers.Internal;

[ApiController]
[Authorize(Policy = Policy.B2CInternal)]
[Route("api/authorization")]
[ApiVersionNeutral]
[ApiExplorerSettings(IgnoreApi = true)]
public class B2CInternalController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Route("userinfo/")]
    [SwaggerOperation(
        Summary = "Retrieves Userinfo from MitID",
        Description = "This endpoint is only used by Azure B2C"
    )]
    public async Task<ActionResult<UserinfoResponse>> GetUserinfo(
        [FromServices] ILogger<B2CInternalController> logger, [FromBody] UserinfoRequest request)
    {
        var queryResult = await mediator.Send(new GetUserinfoFromMitIdQuery(request.MitIDBearerToken));

        return Ok(new UserinfoResponse(
            queryResult.Sub,
            queryResult.Name,
            queryResult.Email,
            queryResult.OrgCvr,
            queryResult.OrgName)
        );
    }

    [HttpPost]
    [Route("client-consent/")]
    [SwaggerOperation(
        Summary = "Retrieves Client Authorization Model",
        Description = "This endpoint is only used by Azure B2C"
    )]
    public async Task<ActionResult<AuthorizationResponse>> GetConsentForClient(
        [FromServices] ILogger<B2CInternalController> logger, [FromBody] AuthorizationClientRequest request)
    {
        var queryResult = await mediator.Send(new GetConsentForClientQuery(request.ClientId));

        return Ok(new AuthorizationResponse(
            queryResult.Sub,
            queryResult.SubType,
            queryResult.OrgName,
            queryResult.OrgId,
            queryResult.OrgIds,
            queryResult.Scope)
        );
    }

    [HttpPost]
    [Route("user-consent/")]
    [SwaggerOperation(
        Summary = "Retrieves User Authorization Model",
        Description = "This endpoint is only used by Azure B2C"
    )]
    public async Task<ActionResult<UserAuthorizationResponse>> GetConsentForUser(
        [FromServices] ILogger<B2CInternalController> logger, [FromBody] AuthorizationUserRequest request)
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
            commandResult.OrgId,
            commandResult.OrgIds,
            commandResult.Scope,
            commandResult.TermsAccepted));
    }

    [HttpPost]
    [Route("whitelisted-organization")]
    [SwaggerOperation(
        Summary = "Gets whether an organization is whitelisted",
        Description = "This endpoint is only used by Azure B2C"
    )]
    public async Task<ActionResult<bool>> GetIsWhitelistedOrganization([FromBody] WhitelistedOrganizationRequest request)
    {
        var isWhitelisted = await mediator.Send(new GetWhitelistedOrganizationQuery(request.OrgCvr, request.LoginType));
        var loginType = request.LoginType.ToLowerInvariant();
        if (isWhitelisted && loginType == "normal" || !isWhitelisted && loginType == "trial")
        {
            return Ok();
        }

        return new ObjectResult(
            new AuthorizationErrorResponse($"Organization not whitelisted {request.LoginType}"))
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }

    [HttpPost]
    [Route("organization-status")]
    [SwaggerOperation(Summary = "Gets the organizations status",
        Description = "This endpoint is only used by Azure B2C")]
    public async Task<ActionResult> GetDoesLoginTypeMatch([FromBody] DoesOrganizationStatusMatchLoginTypeRequest request)
    {
        var queryHandlerResult = await mediator.Send(new GetOrganizationStatusQuery(request.OrgCvr, request.LoginType));
        var loginType = request.LoginType.ToLowerInvariant();

        if (queryHandlerResult.IsValid)
            return Ok();

        var failureGuid = (queryHandlerResult.OrgStatus, loginType) switch
        {
            (OrganizationStatus.Trial, "normal") => LoginFailureReasons.TrialOrganizationIsNotAllowedToLogInAsNormalOrganization,
            (OrganizationStatus.Normal, "trial") => LoginFailureReasons.NormalOrganizationsAreNotAllowedToLogInAsTrial,
            (OrganizationStatus.Deactivated, _) => LoginFailureReasons.OrganizationIsDeactivated,
            (_, "normal") or (_, "trial") => LoginFailureReasons.UnknownLoginTypeSpecifiedInRequest,
            _ => LoginFailureReasons.UnhandledException
        };

        var response = new AuthorizationErrorResponse(
            UserMessage: failureGuid,
            Version: "1.0",
            Status: 409);

        return new ObjectResult(response)
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
