using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

[Authorize]
public class TrialOrganizationsController : Controller
{
    private readonly IMediator _mediator;

    public TrialOrganizationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var query = new GetTrialOrganizationsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return View(result.Organizations);
    }
}
