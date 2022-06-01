using System.ComponentModel.DataAnnotations;
using API.Models;
using API.Services;
using EnergyOriginAuthorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
//[Route("[controller]")]
[Authorize]
public class EmissionsController : AuthorizationController
{
    readonly ILogger<EmissionsController> logger;
    readonly IEmissionsService emissionsService;

    public EmissionsController(ILogger<EmissionsController> logger, IEmissionsService emissionsService)
    {
        this.logger = logger;
        this.emissionsService = emissionsService;
    }

    [HttpGet]
    [Route("emissions")]
    public async Task<IEnumerable<Emissions>> GetEmissions(
        [Required] long dateFrom,
        [Required] long dateTo,
        Aggregation aggregation = Aggregation.Total)
    {

        // Validation

        return await emissionsService.GetTotalEmissions(Context, dateFrom, dateTo, aggregation);
    }
}