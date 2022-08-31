using API.EnergySources.Models;
using API.Features.EnergySources;
using EnergyOriginAuthorization;
using EnergyOriginDateTimeExtension;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
public class EnergySourcesController : AuthorizationController
{
    private readonly ISourcesService emissionsService;
    private readonly IValidator<EnergySourceRequest> validator;

    public EnergySourcesController(ISourcesService emissionsService, IValidator<EnergySourceRequest> validator)
    {
        this.emissionsService = emissionsService;
        this.validator = validator;
    }

    [HttpGet]
    [Route("sources")]
    public async Task<ActionResult<EnergySourceResponse>> GetEnergySources([FromQuery] EnergySourceRequest request)
    {
        var result = await validator.ValidateAsync(request);
        if (!result.IsValid)
        {
            result.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var dateFromDateTime = request.DateFrom.ToDateTime();
        var dateToDateTime = request.DateTo.ToDateTime();

        return Ok(await emissionsService.GetSourceDeclaration(Context, dateFromDateTime, dateToDateTime, request.Aggregation));
    }
}
