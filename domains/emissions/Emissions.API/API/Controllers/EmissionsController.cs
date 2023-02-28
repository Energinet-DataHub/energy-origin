using API.Models;
using API.Services;
using EnergyOriginAuthorization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
public class EmissionsController : AuthorizationController
{
    [HttpGet]
    [Route("api/emissions")]
    public async Task<ActionResult<EmissionsResponse>> GetEmissions([FromQuery] EnergySourceRequest request, IEmissionsService emissionsService, IValidator<EnergySourceRequest> validator)
    {
        var result = await validator.ValidateAsync(request);
        if (!result.IsValid)
        {
            result.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        return Ok(await emissionsService.GetTotalEmissions(
            Context,
            DateTimeOffset.FromUnixTimeSeconds(request.DateFrom),
            DateTimeOffset.FromUnixTimeSeconds(request.DateTo),
            request.TimeZoneInfo,
            request.Aggregation));
    }

    [HttpGet]
    [Route("api/sources")]
    public async Task<ActionResult<EnergySourceResponse>> GetEnergySources([FromQuery] EnergySourceRequest request, IEmissionsService emissionsService, IValidator<EnergySourceRequest> validator)
    {
        var result = await validator.ValidateAsync(request);
        if (!result.IsValid)
        {
            result.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        return Ok(await emissionsService.GetSourceDeclaration(
            Context,
            DateTimeOffset.FromUnixTimeSeconds(request.DateFrom),
            DateTimeOffset.FromUnixTimeSeconds(request.DateTo),
            request.TimeZoneInfo,
            request.Aggregation));
    }
}
