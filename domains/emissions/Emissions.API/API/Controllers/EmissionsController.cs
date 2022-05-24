using System.ComponentModel.DataAnnotations;
using API.Models;
using API.Services;
using EnergyOriginAuthorization;
using Microsoft.AspNetCore.Mvc;
using EnergyOriginAuthorization;
using API.Helpers;

namespace API.Controllers;

namespace DataSync.API.Controllers
{
[ApiController]
[Route("[controller]")]
//[Authorize]
public class EmissionsController : AuthorizationController
{
    private readonly ILogger _logger;
    private readonly ISourceDeclarationService _sourceDeclarationService;
    private readonly IEmissionsService _emissionsService;

    public EmissionsController(ILogger logger, ISourceDeclarationService sourceDeclarationService, IEmissionsService emissionsService)
    {
        _logger = logger;
        _sourceDeclarationService = sourceDeclarationService;
        _emissionsService = emissionsService;
    }

    [HttpGet]
    [Route("sources")]
    public async Task<IEnumerable<GetEnergySourcesResponse>> GetEnergySources(
        [Required] long dateFrom, 
        [Required] long dateTo, 
        Aggregation aggregation = Aggregation.Actual)
    {
        // Validation

        return await _sourceDeclarationService.GetSourceDeclaration(dateFrom, dateTo, aggregation);
    }
    
    [HttpGet]
    [Route("emissions")]
    public async Task<IEnumerable<GetEmissionsResponse>> GetEmissions(
        [Required] long dateFrom, 
        [Required] long dateTo, 
        Aggregation aggregation = Aggregation.Actual)
    {
        
        // Validation

        return await _emissionsService.GetEmissions(Context ,dateFrom, dateTo, aggregation);
    }
}
