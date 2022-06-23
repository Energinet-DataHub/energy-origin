using API.Models;
using API.Models.Request;
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

    /// <summary>
    /// Get consumption measurements for all metering point connected to current user
    /// </summary>
    [HttpGet]
    [Route("measurements/consumption")]
    public async Task<MeasurementResponse> GetMeasurements([FromQuery] MeasurementsRequest request)
    {
        return await measurementsService.GetConsumption(Context, request.DateFrom, request.DateTo, request.Aggregation);
    }
}
