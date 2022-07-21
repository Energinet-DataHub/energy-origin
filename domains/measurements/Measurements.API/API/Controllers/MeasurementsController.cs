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
    private readonly IMeasurementsService _measurementsService;
    private readonly IValidator<MeasurementsRequest> _validator;

    public MeasurementsController(IMeasurementsService measurementsService, IValidator<MeasurementsRequest> validator)
    {
        _measurementsService = measurementsService;
        _validator = validator;
    }

    [HttpGet]
    [Route("measurements/consumption")]
    public async Task<IActionResult> GetConsumptionMeasurements([FromQuery] MeasurementsRequest request)
    {
        var validateResult = await _validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState, null);
            return BadRequest(ModelState);
        }

        return Ok(await _measurementsService.GetMeasurements(Context, request.DateFrom, request.DateTo, request.Aggregation));
    }

    [HttpGet]
    [Route("measurements/production")]
    public async Task<IActionResult> GetProductionMeasurements([FromQuery] MeasurementsRequest request)
    {
        var validateResult = await _validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState, null);
            return BadRequest(ModelState);
        }

        return Ok(await _measurementsService.GetMeasurements(Context, request.DateFrom, request.DateTo, request.Aggregation));
    }
}
