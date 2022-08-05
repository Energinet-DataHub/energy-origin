using API.Models;
using API.Models.Request;
using API.Services;
using EnergyOriginAuthorization;
using EnergyOriginDateTimeExtension;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
public class MeasurementsController : AuthorizationController
{
    readonly IMeasurementsService measurementsService;
    readonly IValidator<MeasurementsRequest> validator;

    public MeasurementsController(IMeasurementsService measurementsService, IValidator<MeasurementsRequest> validator)
    {
        this.measurementsService = measurementsService;
        this.validator = validator;
    }

    [HttpGet]
    [Route("measurements/consumption")]
    public async Task<ActionResult<MeasurementResponse>> GetConsumptionMeasurements([FromQuery] MeasurementsRequest request)
    {
        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var dateFromDateTime = request.DateFrom.ToDateTime();
        var dateToDateTime = request.DateTo.ToDateTime();

        var TypeOfMP = MeterType.Consumption;

        return Ok(await measurementsService.GetMeasurements(Context, dateFromDateTime, dateToDateTime, request.Aggregation, TypeOfMP));
    }

    [HttpGet]
    [Route("measurements/production")]
    public async Task<ActionResult<MeasurementResponse>> GetProductionMeasurements([FromQuery] MeasurementsRequest request)
    {
        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var dateFromDateTime = request.DateFrom.ToDateTime();
        var dateToDateTime = request.DateTo.ToDateTime();

        var TypeOfMP = MeterType.Production;

        return Ok(await measurementsService.GetMeasurements(Context, dateFromDateTime, dateToDateTime, request.Aggregation, TypeOfMP));
    }
}
