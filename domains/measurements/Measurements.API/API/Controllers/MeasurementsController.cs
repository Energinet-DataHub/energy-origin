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

    [HttpGet]
    [Route("api/measurements/consumption")]
    public async Task<ActionResult<MeasurementResponse>> GetConsumptionMeasurements([FromQuery] MeasurementsRequest request, IMeasurementsService measurementsService, IValidator<MeasurementsRequest> validator, [FromServices] IHttpContextAccessor httpContextAccessor)
    {
        var authenticationHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString() ?? throw new PropertyMissingException(nameof(httpContextAccessor.HttpContext));
        var bearerToken = AuthenticationHeaderValue.Parse(authenticationHeader);

        var validateResult = await validator.ValidateAsync(request);

        if (validateResult.IsValid)
            return Ok(await measurementsService.GetMeasurements(
                bearerToken,
                request.TimeZoneInfo,
                DateTimeOffset.FromUnixTimeSeconds(request.DateFrom),
                DateTimeOffset.FromUnixTimeSeconds(request.DateTo),
                request.Aggregation,
                MeterType.Consumption));

        validateResult.AddToModelState(ModelState);
        return ValidationProblem(ModelState);
    }

    [HttpGet]
    [Route("api/measurements/production")]
    public async Task<ActionResult<MeasurementResponse>> GetProductionMeasurements(
        [FromQuery] MeasurementsRequest request,
        IMeasurementsService measurementsService,
        IValidator<MeasurementsRequest> validator,
        [FromServices] IHttpContextAccessor httpContextAccessor
        )
    {
        var authenticationHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString() ?? throw new PropertyMissingException(nameof(httpContextAccessor.HttpContext));
        var bearerToken = AuthenticationHeaderValue.Parse(authenticationHeader);

        var validateResult = await validator.ValidateAsync(request);

        if (validateResult.IsValid)
            return Ok(await measurementsService.GetMeasurements(
                bearerToken,
                request.TimeZoneInfo,
                DateTimeOffset.FromUnixTimeSeconds(request.DateFrom),
                DateTimeOffset.FromUnixTimeSeconds(request.DateTo),
                request.Aggregation,
                MeterType.Production));

        validateResult.AddToModelState(ModelState);
        return ValidationProblem(ModelState);
    }
}
