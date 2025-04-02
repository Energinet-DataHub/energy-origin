using System;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Dtos.Request;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

// [Authorize]
public class WhitelistedOrganizationsController : Controller
{
    private readonly IMediator _mediator;

    public WhitelistedOrganizationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var query = new GetWhitelistedOrganizationsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return View(result.ViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("WhitelistedOrganizations")]
    public async Task<IActionResult> WhitelistFirstPartyOrganization([FromForm] AddOrganizationToWhitelistRequest request)
    {
        try
        {
            var command = new AddOrganizationToWhitelistCommand(Tin.Create(request.Tin));
            await _mediator.Send(command);

            TempData["SuccessMessage"] = $"Organization with TIN {request.Tin} has been successfully added to the whitelist.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to add organization: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), "WhitelistedOrganizations");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("WhitelistedOrganizations/remove")]
    public async Task<IActionResult> RemoveOrganizationFromWhitelist([FromForm] string tin)
    {
        try
        {
            var cmd = new RemoveOrganizationFromWhitelistCommand(tin);
            await _mediator.Send(cmd);
            TempData["SuccessMessage"] = "Organization has successfully been removed from whitelist.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to remove organization: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), "WhitelistedOrganizations");
    }
}
