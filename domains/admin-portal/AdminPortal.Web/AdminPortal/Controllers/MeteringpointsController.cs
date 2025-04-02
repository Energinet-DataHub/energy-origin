using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

public class MeteringpointsController(IMediator mediator) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var query = new GetMeteringPointsQuery();
        var result = await mediator.Send(query, cancellationToken);
        return View(result.ViewModel);
    }

}
