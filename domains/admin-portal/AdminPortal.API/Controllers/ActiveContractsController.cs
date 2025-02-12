using AdminPortal.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.API.Controllers;

[Authorize]
public class ActiveContractsController : Controller
{
    private readonly IAggregationService _aggregationService;

    public ActiveContractsController(IAggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }

    public async Task<IActionResult> Index()
    {
        var response = await _aggregationService.GetActiveContractsAsync();
        return View(response.Results.MeteringPoints);
    }
}
