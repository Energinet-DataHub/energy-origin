using System;
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
[Authorize(Policy = Policy.B2CPolicy)]
[ApiVersion(ApiVersions.Version20230101)]
public class ClientController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retreives Client.
    /// </summary>
    [HttpGet]
    [Route("api/authorization/client/{idpClientId}")]
    public async Task<ActionResult<ClientResponse>> GetClient([FromServices] ILogger<ClientResponse> logger, [FromRoute] Guid idpClientId)
    {
        var queryResult = await _mediator.Send(new GetlientQuery(new IdpClientId(idpClientId)));
        return Ok(new ClientResponse(queryResult.IdpClientId.Value, queryResult.Name.Value, queryResult.RedirectUrl));
    }
}
