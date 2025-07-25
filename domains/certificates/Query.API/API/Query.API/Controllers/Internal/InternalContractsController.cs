using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Internal;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Http;
using MediatR;
using API.Query.API.ApiModels.Responses;

namespace API.Query.API.Controllers.Internal;

[ApiController]
[Authorize(Policy = Policy.AdminPortal)]
[ApiVersionNeutral]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/certificates/admin-portal/internal-contracts")]
public class InternalContractsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ContractsForAdminPortalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ContractsForAdminPortalResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetContractsForAdminPortal(CancellationToken cancellationToken)
    {
        var queryResult = await mediator.Send(new GetContractsForAdminPortalQuery(), cancellationToken);

        var responseItems = queryResult.Result
            .Select(c => new ContractsForAdminPortalResponseItem(
                c.GSRN,
                c.MeteringPointOwner,
                c.Created.ToUnixTimeSeconds(),
                c.StartDate.ToUnixTimeSeconds(),
                c.EndDate?.ToUnixTimeSeconds(),
                c.MeteringPointType
            )).ToList();

        return Ok(new ContractsForAdminPortalResponse(responseItems));
    }
}
