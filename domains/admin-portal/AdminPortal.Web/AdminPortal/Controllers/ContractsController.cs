using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdminPortal._Features_;
using AdminPortal.Dtos.Request;
using EnergyOrigin.Setup.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

// TODO: CABOL - We are not sending back created information in the post response

// [Authorize] TODO: CABOL - Enable again
[Route("contracts")]
public class ContractsController(IMediator mediator) : Controller
{
    /// <summary>
    /// Create contracts that activates granular certificate generation for a metering point
    /// </summary>
    // [ValidateAntiForgeryToken] TODO: CABOL - Enable again
    [HttpPost]
    public async Task<ActionResult> CreateContract([FromBody] CreateContracts createContracts, CancellationToken cancellationToken)
    {
        var command = new CreateContractCommand
        {
            Contracts = [.. createContracts.Contracts.Select(x => new CreateContractItem { Gsrn = x.GSRN, StartDate = x.StartDate, EndDate = x.EndDate })],
            MeteringPointOwnerId = createContracts.MeteringPointOwnerId,
        };

        try
        {
            var result = await mediator.Send(command, cancellationToken);
            return Created(string.Empty, result);
        }
        catch (ContractsException ex)
        {
            return new ObjectResult(new { message = ex.Message })
            {
                StatusCode = (int)ex.StatusCode
            };
        }
    }

    /// <summary>
    /// Edit the end date for multiple contracts
    /// </summary>
    // [ValidateAntiForgeryToken] TODO: CABOL - Enable again
    [HttpPut]
    public async Task<ActionResult> UpdateEndDate([FromBody] EditContracts editContracts, CancellationToken cancellationToken)
    {
        var command = new EditContractCommand
        {
            Contracts = [.. editContracts.Contracts.Select(x => new EditContractItem { Id = x.Id, EndDate = x.EndDate })],
            MeteringPointOwnerId = editContracts.MeteringPointOwnerId,
        };

        try
        {
            await mediator.Send(command, cancellationToken);
            return Ok();
        }
        catch (ContractsException ex)
        {
            return new ObjectResult(new { message = ex.Message })
            {
                StatusCode = (int)ex.StatusCode
            };
        }
    }
}
