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
    private readonly ISourceDeclarationService _sourceDeclarationService;

    public EmissionsController(ISourceDeclarationService sourceDeclarationService)
    {
        _sourceDeclarationService = sourceDeclarationService;
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
}
