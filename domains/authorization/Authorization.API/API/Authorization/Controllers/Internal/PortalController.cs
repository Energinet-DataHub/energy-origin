using System.Threading;
using System.Threading.Tasks;
using API.Authorization._Features_.Internal;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace API.Authorization.Controllers.Internal;

[ApiController]
[Route("api/portal/organizations")]
public class PortalController : ControllerBase
{
    private readonly IMediator _mediator;

    public PortalController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrganizations(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetOrganizationsQueryRequest(), cancellationToken);
        return Ok(response);
    }
}
