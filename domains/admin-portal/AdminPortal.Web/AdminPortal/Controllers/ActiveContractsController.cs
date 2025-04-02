using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

// [Authorize]
public class ActiveContractsController : Controller
{
    private readonly IMediator _mediator;

    public ActiveContractsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var query = new GetActiveContractsQuery();
        var response = await _mediator.Send(query, cancellationToken);
        return View(response.Results.MeteringPoints);
    }
}
