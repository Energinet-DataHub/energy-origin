using System.Threading.Tasks;
using AdminPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

[Authorize]
public class WhitelistedOrganizationsController : Controller
{
    private readonly IWhitelistedOrganizationsQuery _whitelistedOrganizationsQuery;

    public WhitelistedOrganizationsController(IWhitelistedOrganizationsQuery whitelistedOrganizationsQuery)
    {
        _whitelistedOrganizationsQuery = whitelistedOrganizationsQuery;
    }

    public async Task<IActionResult> Index()
    {
        var whitelistedOrganizations = await _whitelistedOrganizationsQuery.GetWhitelistedOrganizationsAsync();
        return View(whitelistedOrganizations);
    }
}
