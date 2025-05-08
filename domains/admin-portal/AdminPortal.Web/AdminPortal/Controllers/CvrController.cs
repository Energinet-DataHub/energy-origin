using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

[Authorize]
[Route("cvr")]
public class CvrController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ValidateAntiForgeryToken]
    [Route("company/{tin}")]
    public async Task<IActionResult> GetCompanyInformation([FromRoute] string tin, CancellationToken cancellationToken)
    {
        var query = new GetCompanyInformationQuery(tin);

        var response = await mediator.Send(query, cancellationToken);
        if (response is null)
        {
            return BadRequest();
        }

        return Ok(response);
    }
}
