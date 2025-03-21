using System;
using System.Threading.Tasks;
using AdminPortal.Dtos.Request;
using AdminPortal.Services;
using MassTransit.Mediator;
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

    public async Task<IActionResult> Index()
    {
        var whitelistedOrganizations = await _whitelistedOrganizationsQuery.GetWhitelistedOrganizationsAsync();
        return View(whitelistedOrganizations);
    }

    [HttpPost]
    public async Task<IActionResult> WhitelistFirstPartyOrganization([FromForm] AddOrganizationToWhitelistRequest request)
    {
        var command = new AddOrganizationToWhitelistCommand { Tin = request.Tin };
        await _mediator.Send(command);

        return RedirectToAction(nameof(Index));
    }
}
