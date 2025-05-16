using System.Threading;
using System.Threading.Tasks;
using AdminPortal.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

[Authorize]
public class MeteringpointsController(IMediator mediator) : Controller
{
    public async Task<IActionResult> Index(string Tin, CancellationToken cancellationToken)
    {
        var query = new GetMeteringPointsQuery(Tin);
        var result = await mediator.Send(query, cancellationToken);
        return View(result.ViewModel);
    }

}
