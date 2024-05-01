using System;
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
public class ConsentController  : ControllerBase
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
    [Authorize]
    [Route("api/consent/grant/")]
    public async Task<ActionResult> GrantConsent([FromServices] ILogger<ConsentController> logger, [FromBody] GrantConsentRequest request)
    {
        // TODO: Only allow sub-type 'user' and get organizationId from request
        await _mediator.Send(new GrantConsentCommand(_entityDescriptor.Sub, Guid.NewGuid(), request.ClientId));
        return Ok();
    }
}
