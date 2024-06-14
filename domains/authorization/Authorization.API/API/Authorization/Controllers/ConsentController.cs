using System;
using System.Linq;
using System.Threading.Tasks;
using API.Authorization._Features_;
using API.ValueObjects;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Authorization.Controllers;

[ApiController]
[ApiVersion(ApiVersions.Version20230101)]
[Authorize(Policy.B2CCvrClaim)]
[Authorize(Policy.B2CSubTypeUserPolicy)]
public class ConsentController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Grants consent.
    /// </summary>
    [HttpPost]
    [Route("api/authorization/consent/grant/")]
    public async Task<ActionResult> GrantConsent([FromServices] ILogger<ConsentController> logger, [FromBody] GrantConsentRequest request)
    {
        var identity = new IdentityDescriptor(HttpContext);

        await mediator.Send(new GrantConsentCommand(identity.Sub, identity.OrgId,
            new IdpClientId(request.IdpClientId)));
        return Ok();
    }

    /// <summary>
    /// Get consent from a specific Client.
    /// </summary>
    [HttpGet]
    [Route("api/authorization/consent/grant/{clientId}")]
    public async Task<ActionResult> GetConsent([FromServices] ILogger<ConsentController> logger, [FromRoute] Guid clientId)
    {
        var result = await mediator.Send(new GetConsentQuery(clientId));
        return Ok(result);
    }

    /// <summary>
    /// Get consent for a specific User, representing an Organization.
    /// </summary>
    [HttpGet]
    [Route("api/authorization/consents/")]
    public async Task<ActionResult> GetConsent([FromServices] ILogger<ConsentController> logger)
    {
        var identity = new IdentityDescriptor(HttpContext);

        var result = await mediator.Send(new GetUserOrganizationConsentsQuery(identity.Sub.ToString(), identity.OrgCvr!));
        return Ok(result);
    }
}
