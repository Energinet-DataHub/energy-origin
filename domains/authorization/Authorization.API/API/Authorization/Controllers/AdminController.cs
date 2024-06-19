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
[Authorize(Policy = Policy.B2CCustomPolicyClientPolicy)]
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
    [Authorize(Policy.B2CCustomPolicyClientPolicy)]
    [Route("api/authorization/Admin/Client")]
    public async Task<ActionResult> CreateClient(
        [FromServices] ILogger<AuthorizationController> logger, [FromBody] CreateClientRequest request)
    {
        var result = await _mediator.Send(new CreateClientCommand(new IdpClientId(request.IdpClientId), new ClientName(request.Name), ClientTypeMapper.MapToDatabaseClientType(request.ClientType), request.RedicrectUrl));

        return Created($"api/authorization/Admin/Client/{result.Id}", result.Id);
    }
}
