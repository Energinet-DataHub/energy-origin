using System.Threading.Tasks;
using AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

[Authorize]
public class ActiveContractsController : Controller
{
    private readonly IAggregationQuery _aggregationQuery;

    public ActiveContractsController(IAggregationQuery aggregationQuery)
    {
        _aggregationQuery = aggregationQuery;
    }

    public async Task<IActionResult> Index()
    {
        var response = await _aggregationQuery.GetActiveContractsAsync();
        return View(response.Results.MeteringPoints);
    }
}
