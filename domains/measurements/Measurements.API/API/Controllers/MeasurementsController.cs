using API.Models;
using API.Models.Request;
using API.Services;
using EnergyOriginAuthorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

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
    [Route("measurements/consumption")]
    public async Task<MeasurementResponse> GetConsumptionMeasurements([FromQuery] MeasurementsRequest request)
    {
        return await measurementsService.GetMeasurements(Context, request.DateFrom, request.DateTo, request.Aggregation);
    }

    [HttpGet]
    [Route("measurements/production")]
    public async Task<MeasurementResponse> GetProductionMeasurements([FromQuery] MeasurementsRequest request)
    {
        return await measurementsService.GetMeasurements(Context, request.DateFrom, request.DateTo, request.Aggregation);
    }
}
