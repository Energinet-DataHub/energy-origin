using AdminPortal.API.Models;
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

    public IActionResult Index()
    {
        var dummyData = new List<AggregatedData>
        {
            new AggregatedData
            {
                Gsrn = "571313171355435420",
                MeteringPointType = "Consumption",
                OrganizationId = "40268e7c-760b-4855-9ee4-916900366b55",
                OrganizationName = "Bolighaj A/S",
                Tin = "55555555"
            },
            new AggregatedData
            {
                Gsrn = "571313130083535430",
                MeteringPointType = "Production",
                OrganizationId = "cffd286e-bd48-49f8-b50a-1ee714d32938",
                OrganizationName = "Producent A/S",
                Tin = "11223344"
            }
        };

        return View(dummyData);
    }

    // public async Task<IActionResult> Index()
    // {
    //     var meteringPoints = await _aggregationService.GetMeteringPointsAsync();
    //     var organizations = await _aggregationService.GetOrganizationsAsync();
    //     var aggregatedData = _aggregationService.AggregateData(meteringPoints, organizations);
    //
    //     return View(aggregatedData);
    // }

    [HttpGet("api/admin/portal/aggregated-information")]
    public async Task<IActionResult> GetAggregatedInformation()
    {
        var meteringPoints = await _aggregationService.GetMeteringPointsAsync();
        var organizations = await _aggregationService.GetOrganizationsAsync();
        var aggregatedData = _aggregationService.AggregateData(meteringPoints, organizations);

        return Ok(aggregatedData);
    }
}
