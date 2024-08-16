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
[Authorize(Policy = Policy.B2CInternal)]
[ApiVersion(ApiVersions.Version20230101)]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create Client.
    /// </summary>
    [HttpPost]
    [Route("api/authorization/admin/client")]
    public async Task<ActionResult> CreateClient(
        [FromServices] ILogger<AuthorizationController> logger, [FromBody] CreateClientRequest request)
    {
        var result = await _mediator.Send(new CreateClientCommand(new IdpClientId(request.IdpClientId), new ClientName(request.Name),
            ClientTypeMapper.MapToDatabaseClientType(request.ClientType), request.RedirectUrl));

        return Created($"api/authorization/admin/client/{result.Id}",
            new CreateClientResponse(result.Id, result.IdpClientId.Value, result.Name.Value, ClientTypeMapper.MapToApiClientType(result.ClientType),
                result.RedirectUrl));
    }
}
