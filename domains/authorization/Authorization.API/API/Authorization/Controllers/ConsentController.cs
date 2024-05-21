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
[Authorize(Policy = Policy.B2CSubTypeUserPolicy)]
public class ConsentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly EntityDescriptor _entityDescriptor;

    public ConsentController(IMediator mediator, EntityDescriptor entityDescriptor)
    {
        _mediator = mediator;
        _entityDescriptor = entityDescriptor;
    }

    /// <summary>
    /// Grants consent.
    /// </summary>
    [HttpPost]
    [Route("api/consent/grant/")]
    public async Task<ActionResult> GrantConsent([FromServices] ILogger<ConsentController> logger,
        [FromBody] GrantConsentRequest request)
    {
        ;
        await _mediator.Send(new GrantConsentCommand(_entityDescriptor.Sub, _entityDescriptor.OrgIds.First(),
            new IdpClientId(request.ClientId)));
        return Ok();
    }

    /// <summary>
    /// Get consent from a specific Client.
    /// </summary>
    [HttpGet]
    [Route("api/consent/grant/{clientId}")]
    public async Task<ActionResult> GetConsent([FromServices] ILogger<ConsentController> logger,
        [FromRoute] Guid clientId)
    {
        var result = await _mediator.Send(new GetConsentQuery(clientId));
        return Ok(result);
    }
}
