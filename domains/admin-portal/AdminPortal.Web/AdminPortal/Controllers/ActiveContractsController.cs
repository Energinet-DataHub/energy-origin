using System.Threading.Tasks;
using AdminPortal._Features_;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

[Authorize]
public class ActiveContractsController : Controller
{
    private readonly IGetActiveContractsQuery _getActiveContractsQuery;

    public ActiveContractsController(IGetActiveContractsQuery getActiveContractsQuery)
    {
        _getActiveContractsQuery = getActiveContractsQuery;
    }

    public async Task<IActionResult> Index()
    {
        var response = await _getActiveContractsQuery.GetActiveContractsQueryAsync();
        return View(response.Results.MeteringPoints);
    }
}
