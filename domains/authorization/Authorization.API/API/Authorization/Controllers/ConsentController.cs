using System;
using System.Linq;
using System.Threading.Tasks;
using API.Authorization._Features_;
using API.ValueObjects;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Authorization.Controllers;

[ApiController]
[ApiVersion(ApiVersions.Version20230101)]
[Authorize(Policy.B2CCvrClaim)]
[Authorize(Policy.B2CSubTypeUserPolicy)]
public class ConsentController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConsentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Grants consent.
    /// </summary>
    [HttpPost]
    [Route("api/authorization/consent/grant/")]
    public async Task<ActionResult> GrantConsent([FromServices] ILogger<ConsentController> logger, [FromBody] GrantConsentRequest request)
    {
        var identity = new IdentityDescriptor(HttpContext);

        await _mediator.Send(new GrantConsentCommand(identity.Sub, identity.OrgId,
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
        var result = await _mediator.Send(new GetConsentQuery(clientId));
        return Ok(result);
    }

    /// <summary>
    /// Get consent from a specific Client.
    /// </summary>
    [HttpGet]
    [Route("api/authorization/consent/grant/")]
    public async Task<ActionResult> GetConsent([FromServices] ILogger<ConsentController> logger)
    {
        var identity = new IdentityDescriptor(HttpContext);
        var result = await _mediator.Send(new GetUserOrganizationConsentsQuery(identity.Sub.ToString()));
        return Ok(result);
    }
}
