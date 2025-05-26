using System;
using System.Linq;
using System.Threading.Tasks;
using API.Authorization._Features_;
using API.ValueObjects;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;

namespace API.Authorization.Controllers;

[ApiController]
[Authorize(Policy = Policy.FrontendOr3rdParty)]
[ApiVersion(ApiVersions.Version1)]
public class ClientController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IdentityDescriptor _identityDescriptor;

    public ClientController(IMediator mediator, IdentityDescriptor identityDescriptor)
    {
        _mediator = mediator;
        _identityDescriptor = identityDescriptor;
    }

    [HttpGet]
    [Route("api/authorization/client/consents")]
    [SwaggerOperation(
        Summary = "Retrieves granted consents for client",
        Description = "Retrieves a list of all consents granted to client identified by bearer token"
    )]
    [ProducesResponseType(typeof(ClientConsentsResponse), 200)]
    public async Task<ActionResult<ClientConsentsResponse>> GetClientConsents([FromServices] ILogger<ClientResponse> logger)
    {
        var queryResult = await _mediator.Send(new GetClientGrantedConsentsQuery(new IdpClientId(_identityDescriptor.Subject)));

        return Ok(new ClientConsentsResponse(queryResult.GetClientConsentsQueryResultItems.Select(x =>
            new ClientConsentsResponseItem(x.OrganizationId, x.OrganizationName.Value, x.Tin?.Value))));
    }

    [HttpGet]
    [Route("api/authorization/client/{idpClientId}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [SwaggerOperation(
        Summary = "Retrieves Client",
        Description = "Retrieves info for client with id idpClientId"
    )]
    public async Task<ActionResult<ClientResponse>> GetClient([FromServices] ILogger<ClientResponse> logger, [FromRoute] Guid idpClientId)
    {
        var queryResult = await _mediator.Send(new GetClientQuery(new IdpClientId(idpClientId)));
        return Ok(new ClientResponse(queryResult.IdpClientId.Value, queryResult.Name.Value, queryResult.RedirectUrl));
    }
}
