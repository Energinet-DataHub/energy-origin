using System.Threading.Tasks;
using API.Authorization._Features_.Internal;
using API.ValueObjects;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Authorization.Controllers.Internal;

[ApiController]
[Authorize(Policy = Policy.B2CInternal)]
[ApiVersion(ApiVersions.Version1)]
[ApiExplorerSettings(IgnoreApi = true)]
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
    public async Task<ActionResult> CreateClient([FromBody] CreateClientRequest request)
    {
        var result = await _mediator.Send(new CreateClientCommand(new IdpClientId(request.IdpClientId), new ClientName(request.Name),
            ClientTypeMapper.MapToDatabaseClientType(request.ClientType), request.RedirectUrl, request.IsTrial));

        return Created($"api/authorization/admin/client/{result.Id}",
            new CreateClientResponse(result.Id, result.IdpClientId.Value, result.Name.Value, ClientTypeMapper.MapToApiClientType(result.ClientType),
                result.RedirectUrl));
    }
}
