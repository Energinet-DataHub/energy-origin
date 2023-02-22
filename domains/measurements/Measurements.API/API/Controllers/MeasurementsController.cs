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
    [HttpGet]
    [Route("api/measurements/consumption")]
    public async Task<ActionResult<MeasurementResponse>> GetConsumptionMeasurements([FromQuery] MeasurementsRequest request, IMeasurementsService measurementsService, IValidator<MeasurementsRequest> validator)
    {
        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var dateFromDateTime = request.DateFrom.ToDateTime();
        var dateToDateTime = request.DateTo.ToDateTime();

        var typeOfMP = MeterType.Consumption;

        return Ok(await measurementsService.GetMeasurements(Context, dateFromDateTime, dateToDateTime, request.Aggregation, typeOfMP));
    }

    [HttpGet]
    [Route("api/measurements/production")]
    public async Task<ActionResult<MeasurementResponse>> GetProductionMeasurements([FromQuery] MeasurementsRequest request, IMeasurementsService measurementsService, IValidator<MeasurementsRequest> validator)
    {
        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var dateFromDateTime = request.DateFrom.ToDateTime();
        var dateToDateTime = request.DateTo.ToDateTime();

        var typeOfMP = MeterType.Production;

        return Ok(await measurementsService.GetMeasurements(Context, dateFromDateTime, dateToDateTime, request.Aggregation, typeOfMP));
    }
}
