using System.Net.Http.Headers;
using API.Models;
using API.Models.Request;
using API.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
public class MeasurementsController : ControllerBase
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public MeasurementsController(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    [Route("api/measurements/consumption")]
    public async Task<ActionResult<MeasurementResponse>> GetConsumptionMeasurements([FromQuery] MeasurementsRequest request, IMeasurementsService measurementsService, IValidator<MeasurementsRequest> validator)
    {
        var bearerToken = AuthenticationHeaderValue.Parse(httpContextAccessor.HttpContext?.Request.Headers["Authorization"]!);

        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        return Ok(await measurementsService.GetMeasurements(
            bearerToken,
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
        var bearerToken = AuthenticationHeaderValue.Parse(httpContextAccessor.HttpContext?.Request.Headers["Authorization"]!);

        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        return Ok(await measurementsService.GetMeasurements(
            bearerToken,
            request.TimeZoneInfo,
            DateTimeOffset.FromUnixTimeSeconds(request.DateFrom),
            DateTimeOffset.FromUnixTimeSeconds(request.DateTo),
            request.Aggregation,
            MeterType.Production));
    }
}
