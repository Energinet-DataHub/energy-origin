using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService.Internal;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DataContext.ValueObjects;
using EnergyOrigin.TokenValidation.b2c;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MediatR;

namespace API.Query.API.Controllers.Internal;

[Authorize(Policy = Policy.B2CInternal)]
[ApiController]
[ApiVersionNeutral]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/certificates/portal")]
public class InternalContractsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InternalContractsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("internal-contracts")]
    [ProducesResponseType(typeof(ContractsForAdminPortalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ContractsForAdminPortalResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetContractsForAdminPortal(
        [FromServices] ILogger<InternalContractsController> logger, CancellationToken cancellationToken)
    {
        var queryResult = await _mediator.Send(new GetContractsForAdminPortalQuery(), cancellationToken);

        var responseItems = queryResult.Result
            .Select(c => new ContractsForAdminPortalResponseItem(
                c.GSRN,
                c.MeteringPointOwner,
                c.Created,
                c.StartDate,
                c.EndDate,
                c.MeteringPointType
            )).ToList();

        return Ok(new ContractsForAdminPortalResponse(responseItems));
    }
}


public record ContractsForAdminPortalResponseItem(
    string GSRN,
    string MeteringPointOwner,
    DateTimeOffset Created,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    MeteringPointType MeteringPointType
);

public record ContractsForAdminPortalResponse(IEnumerable<ContractsForAdminPortalResponseItem> Result);
