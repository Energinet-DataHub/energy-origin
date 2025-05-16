using System.Threading;
using System.Threading.Tasks;
using API.Cvr.Api._Features_.Internal;
using API.Cvr.Api.Dto.Responses.Internal;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Cvr.Api.Controllers.Internal;

[ApiController]
[Authorize(Policy = Policy.AdminPortal)]
[ApiVersionNeutral]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/transfer/admin-portal")]
public class InternalCvrController(IMediator mediator) : ControllerBase
{
    [ProducesResponseType(typeof(CvrCompanyInformationDto), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet]
    [Route("internal-cvr/companies/{tin}")]
    public async Task<ActionResult<CvrCompanyInformationDto>> GetCvrCompanies([FromRoute] string tin,
        CancellationToken cancellationToken)
    {
        var companyQueryResult = await mediator.Send(new GetCvrCompanyQuery(tin), cancellationToken);
        if (companyQueryResult is null)
        {
            return NotFound();
        }

        return Ok(new CvrCompanyInformationDto
        {
            Name = companyQueryResult.Name,
            Tin = companyQueryResult.Tin,
            Address = companyQueryResult.Address,
            City = companyQueryResult.City,
            ZipCode = companyQueryResult.ZipCode
        });
    }
}
