using System.ComponentModel.DataAnnotations;
using API.Models;
using API.Services;
using EnergyOriginAuthorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class EmissionsController : AuthorizationController
{
    readonly ILogger logger;
    readonly ISourceDeclarationService sourceDeclarationService;
    readonly IEmissionsService emissionsService;

    public EmissionsController(ILogger logger, ISourceDeclarationService sourceDeclarationService,
        IEmissionsService emissionsService)
    {
        this.logger = logger;
        this.sourceDeclarationService = sourceDeclarationService;
        this.emissionsService = emissionsService;
    }

    [HttpGet]
    [Route("sources")]
    public async Task<IEnumerable<GetEnergySourcesResponse>> GetEnergySources(
        [Required] long dateFrom,
        [Required] long dateTo,
        Aggregation aggregation = Aggregation.Actual)
    {
        // Validation

        return await sourceDeclarationService.GetSourceDeclaration(dateFrom, dateTo, aggregation);
    }

    [HttpGet]
    [Route("emissions")]
    public async Task<IEnumerable<Emissions>> GetEmissions(
        [Required] long dateFrom,
        [Required] long dateTo,
        Aggregation aggregation = Aggregation.Total)
    {

        // Validation

        return await emissionsService.GetTotalEmissions(Context, dateFrom, dateTo, aggregation);
    }
}