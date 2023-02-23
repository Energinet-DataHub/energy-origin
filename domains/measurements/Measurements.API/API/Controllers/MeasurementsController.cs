using API.Models;
using API.Models.Request;
using API.Services;
using EnergyOriginAuthorization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
public class MeasurementsController : AuthorizationController
{
    [HttpGet]
    [Route("api/measurements/consumption")]
    public async Task<ActionResult<MeasurementResponse>> GetConsumptionMeasurements([FromQuery] MeasurementsRequest request, IMeasurementsService measurementsService, IValidator<MeasurementsRequest> validator)
    {
        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        return Ok(await measurementsService.GetMeasurements(
            Context,
            request.TimeZoneInfo,
            DateTimeOffset.FromUnixTimeSeconds(request.DateFrom),
            DateTimeOffset.FromUnixTimeSeconds(request.DateTo),
            request.Aggregation,
             MeterType.Consumption));
    }

    [HttpGet]
    [Route("api/measurements/production")]
    public async Task<ActionResult<MeasurementResponse>> GetProductionMeasurements([FromQuery] MeasurementsRequest request, IMeasurementsService measurementsService, IValidator<MeasurementsRequest> validator)
    {
        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        return Ok(await measurementsService.GetMeasurements(
            Context,
            request.TimeZoneInfo,
            DateTimeOffset.FromUnixTimeSeconds(request.DateFrom),
            DateTimeOffset.FromUnixTimeSeconds(request.DateTo),
            request.Aggregation,
            MeterType.Production));
    }
}
