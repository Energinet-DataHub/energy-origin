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
public class AdminController(IMediator Mediator) : ControllerBase
{
    /// <summary>
    /// Create Client.
    /// </summary>
    [HttpPost]
    [Route("api/authorization/Admin/Client")]
    public async Task<ActionResult> CreateClient(
        [FromServices] ILogger<AuthorizationController> logger, [FromBody] CreateClientRequest request)
    {
        var result = await Mediator.Send(new CreateClientCommand(new IdpClientId(request.IdpClientId), new ClientName(request.Name), ClientTypeMapper.MapToDatabaseClientType(request.ClientType), request.RedicrectUrl));

        return Ok();
    }
}
