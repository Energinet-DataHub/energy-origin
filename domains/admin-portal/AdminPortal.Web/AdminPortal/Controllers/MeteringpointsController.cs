using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Dtos.Request;
using EnergyOrigin.Setup.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AdminPortal.Controllers;

[Authorize]
public class MeteringpointsController(IMediator mediator, ILogger<MeteringpointsController> _logger) : Controller
{
    public async Task<IActionResult> Index(string Tin, CancellationToken cancellationToken)
    {
        var query = new GetMeteringPointsQuery(Tin);
        var result = await mediator.Send(query, cancellationToken);
        _logger.LogInformation("THIS LOGGER: " + System.Text.Json.JsonSerializer.Serialize(result.ViewModel.MeteringPoints));

        return View(result.ViewModel);
    }

    /// <summary>
    /// Create contracts that activates granular certificate generation for a metering point
    /// </summary>
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<ActionResult> CreateContract([FromForm] List<string> Gsrn, [FromForm] Guid owner, [FromForm] string tin, CancellationToken cancellationToken)
    {
        var command = new CreateContractCommand
        {
            Contracts = [.. Gsrn.Select(x => new CreateContractItem { Gsrn = x, StartDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds() })],
            MeteringPointOwnerId = owner,
        };

        try
        {
            await mediator.Send(command, cancellationToken);
            return RedirectToAction(nameof(Index), "Meteringpoints", new { Tin = tin });
        }
        catch (ContractsException ex)
        {
            return new ObjectResult(new { message = ex.Message })
            {
                StatusCode = (int)ex.StatusCode
            };
        }
    }

    /// <summary>
    /// Edit the end date for multiple contracts
    /// </summary>
    [ValidateAntiForgeryToken]
    [HttpPut]
    public async Task<ActionResult> UpdateEndDate([FromForm] List<Guid> contractIds, [FromForm] Guid owner, [FromForm] string tin, CancellationToken cancellationToken)
    {
        var command = new EditContractCommand
        {
            Contracts = [.. contractIds.Select(x => new EditContractItem { Id = x, EndDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds() })],
            MeteringPointOwnerId = owner,
        };

        try
        {
            await mediator.Send(command, cancellationToken);
            return RedirectToAction(nameof(Index), "Meteringpoints", new { Tin = tin });
        }
        catch (ContractsException ex)
        {
            return new ObjectResult(new { message = ex.Message })
            {
                StatusCode = (int)ex.StatusCode
            };
        }
    }

}
