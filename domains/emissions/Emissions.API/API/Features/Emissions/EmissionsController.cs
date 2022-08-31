using API.Emissions.Models;
using API.Features.Emissions;
using EnergyOriginAuthorization;
using EnergyOriginDateTimeExtension;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
public class EmissionsController : AuthorizationController
{
    private readonly IEmissionsService emissionsService;
    private readonly IValidator<EmissionsRequest> validator;

    public EmissionsController(IEmissionsService emissionsService, IValidator<EmissionsRequest> validator)
    {
        this.emissionsService = emissionsService;
        this.validator = validator;
    }

    [HttpGet]
    [Route("emissions")]
    public async Task<ActionResult<EmissionsResponse>> GetEmissions([FromQuery] EmissionsRequest request)
    {
        var result = await validator.ValidateAsync(request);
        if (!result.IsValid)
        {
            result.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var dateFromDateTime = request.DateFrom.ToDateTime();
        var dateToDateTime = request.DateTo.ToDateTime();

        return Ok(await emissionsService.GetTotalEmissions(Context, dateFromDateTime, dateToDateTime, request.Aggregation));
    }
}
