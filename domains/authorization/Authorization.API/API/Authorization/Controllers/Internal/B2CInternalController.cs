using System.Threading.Tasks;
using API.Authorization._Features_.Internal;
using API.Models;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Authorization.Controllers.Internal;

[ApiController]
[Authorize(Policy = Policy.B2CInternal)]
[Route("api/authorization")]
[ApiVersionNeutral]
public class B2CInternalController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Route("userinfo/")]
    [SwaggerOperation(
        Summary = "Retrieves Userinfo from MitID",
        Description = "This endpoint is only used by Azure B2C"
    )]
    public async Task<ActionResult<UserinfoResponse>> GetUserinfo(
        [FromBody] UserinfoRequest request)
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
        [FromBody] AuthorizationClientRequest request)
    {
        var queryResult = await mediator.Send(new GetConsentForClientQuery(request.ClientId));

        return Ok(new AuthorizationResponse(
            queryResult.Sub,
            queryResult.SubType,
            queryResult.OrgName,
            queryResult.OrgId,
            queryResult.OrgIds,
            queryResult.Scope,
            queryResult.OrgStatus.ToString().ToLowerInvariant())
        );
    }

    [HttpPost]
    [Route("user-consent/")]
    [SwaggerOperation(
        Summary = "Retrieves User Authorization Model",
        Description = "This endpoint is only used by Azure B2C"
    )]
    public async Task<ActionResult<UserAuthorizationResponse>> GetConsentForUser(
        [FromBody] AuthorizationUserRequest request)
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

        if (queryHandlerResult.IsAllowedAccess)
            return Ok(new DoesOrganizationStatusMatchLoginTypeResponse(queryHandlerResult.GrantedAccessAsTypeOf.ToString()!.ToLowerInvariant()));

        var failureGuid = (OrgStatus: queryHandlerResult.GrantedAccessAsTypeOf, loginType) switch
        {
            (Models.OrganizationStatus.Trial, "normal") => LoginFailureReasons.TrialOrganizationIsNotAllowedToLogInAsNormalOrganization,
            (Models.OrganizationStatus.Normal, "trial") => LoginFailureReasons.NormalOrganizationsAreNotAllowedToLogInAsTrial,
            (Models.OrganizationStatus.Deactivated, _) => LoginFailureReasons.OrganizationIsDeactivated,
            (_, "normal") or (_, "trial") => LoginFailureReasons.UnknownLoginTypeSpecifiedInRequest,
            _ => LoginFailureReasons.UnhandledException
        };

        return new ObjectResult(
            new AuthorizationErrorResponse($"{failureGuid}"))
        {
            StatusCode = StatusCodes.Status409Conflict
        };
    }
}
