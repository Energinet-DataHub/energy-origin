using System.ComponentModel.DataAnnotations;
using API.Models;
using API.Services;
using EnergyOriginAuthorization;
using EnergyOriginDateTimeExtension;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
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
    public async Task<EmissionsResponse> GetEmissions(
        [Required] long dateFrom,
        [Required] long dateTo,
        Aggregation aggregation = Aggregation.Total)
    {
        //TODO: Validation

        var dateFromDateTime = dateFrom.ToDateTime();
        var dateToDateTime = dateTo.ToDateTime();

        return await emissionsService.GetTotalEmissions(Context, dateFromDateTime, dateToDateTime, aggregation);
    }

    [HttpGet]
    [Route("sources")]
    public async Task<EnergySourceResponse> GetEnergySources(
        [Required] long dateFrom,
        [Required] long dateTo,
        Aggregation aggregation = Aggregation.Total)
    {
        //TODO: Validation

        var dateFromDateTime = dateFrom.ToDateTime();
        var dateToDateTime = dateTo.ToDateTime();

        return await emissionsService.GetSourceDeclaration(Context, dateFromDateTime, dateToDateTime, aggregation);
    }
}
