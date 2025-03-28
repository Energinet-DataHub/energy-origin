using System;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Dtos.Request;
using EnergyOrigin.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

[Authorize]
public class WhitelistedOrganizationsController : Controller
{
    private readonly IWhitelistedOrganizationsQuery _whitelistedOrganizationsQuery;
    private readonly IMediator _mediator;

    public WhitelistedOrganizationsController(IWhitelistedOrganizationsQuery whitelistedOrganizationsQuery, IMediator mediator)
    {
        _whitelistedOrganizationsQuery = whitelistedOrganizationsQuery;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var whitelistedOrganizations = await _whitelistedOrganizationsQuery.GetWhitelistedOrganizationsAsync();
        return View(whitelistedOrganizations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("WhitelistedOrganizations")]
    public async Task<IActionResult> WhitelistFirstPartyOrganization([FromForm] AddOrganizationToWhitelistRequest request)
    {
        try
        {
            var command = new AddOrganizationToWhitelistCommand { Tin = Tin.Create(request.Tin) };
            await _mediator.Send(command);

            TempData["SuccessMessage"] = $"Organization with TIN {request.Tin} has been successfully added to the whitelist.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to add organization: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), "WhitelistedOrganizations");
    }
}
