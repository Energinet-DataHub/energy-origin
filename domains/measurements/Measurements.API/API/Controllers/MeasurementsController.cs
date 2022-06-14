using System.ComponentModel.DataAnnotations;
using API.Models;
using API.Services;
using EnergyOriginAuthorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
public class MeasurementsController : AuthorizationController
{
    readonly ILogger<MeasurementsController> logger;
    readonly IMeasurementsService measurementsService;

    public MeasurementsController(ILogger<MeasurementsController> logger, IMeasurementsService measurementsService)
    {
        this.logger = logger;
        this.measurementsService = measurementsService;
    }

    [HttpGet]
    [Route("measurements/consumptions")]
    public async Task<ConsumptionsResponse> GetMeasurements(
        [Required] long dateFrom,
        [Required] long dateTo,
        Aggregation aggregation = Aggregation.Total)
    {
        // TODO: Validation

        return await measurementsService.GetConsumptions(Context, dateFrom, dateTo, aggregation);
    }
}
