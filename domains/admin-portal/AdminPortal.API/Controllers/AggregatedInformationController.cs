using AdminPortal.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.API.Controllers;



public class AggregatedInformationController : Controller
{
    private readonly AggregationService _aggregationService;

    public AggregatedInformationController(AggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }

    public async Task<IActionResult> Index()
    {
        var meteringPoints = await _aggregationService.GetMeteringPointsAsync();
        var organizations = await _aggregationService.GetOrganizationsAsync();
        var aggregatedData = _aggregationService.AggregateData(meteringPoints, organizations);

        return View(aggregatedData);
    }

    [HttpGet("api/admin/portal/aggregated-information")]
    public async Task<IActionResult> GetAggregatedInformation()
    {
        var meteringPoints = await _aggregationService.GetMeteringPointsAsync();
        var organizations = await _aggregationService.GetOrganizationsAsync();
        var aggregatedData = _aggregationService.AggregateData(meteringPoints, organizations);

        return Ok(aggregatedData);
    }
}
