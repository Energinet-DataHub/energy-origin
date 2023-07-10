using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
using API.Query.API.ApiModels.Requests;
using API.Query.API.ApiModels.Responses;
using FluentValidation;
using FluentValidation.AspNetCore;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static API.ContractService.CreateContractResult;
using static API.ContractService.EndContractResult;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class ContractsController : ControllerBase
{
    /// <summary>
    /// Create a contract that activates granular certificate generation for a metering point
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(typeof(void), 409)]
    [Route("api/certificates/contracts")]
    public async Task<ActionResult> CreateContract(
        [FromBody] CreateContract createContract,
        [FromServices] IValidator<CreateContract> validator,
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        var meteringPointOwner = User.FindFirstValue("subject")!;

        var validationResult = await validator.ValidateAsync(createContract, cancellationToken);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        //TODO: This is wrong
        var endDate = createContract.EndDate == null ? DateTimeOffset.Now : DateTimeOffset.FromUnixTimeSeconds((long)createContract.EndDate);

        var result = await service.Create(createContract.GSRN, meteringPointOwner!,
            DateTimeOffset.FromUnixTimeSeconds(createContract.StartDate),
            endDate,
            cancellationToken);

        return result switch
        {
            GsrnNotFound => ValidationProblem($"GSRN {createContract.GSRN} not found"),
            NotProductionMeteringPoint => ValidationProblem($"GSRN {createContract.GSRN} is not a production metering point"),
            ContractAlreadyExists => Conflict(),
            Success(var createdContract) => CreatedAtRoute(
                "GetContract",
                new { id = createdContract.Id },
                Contract.CreateFrom(createdContract)),
            _ => throw new NotImplementedException($"{result.GetType()} not handled by {nameof(ContractsController)}")
        };
    }

    /// <summary>
    /// Returns contract based on the id
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Contract), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [Route("api/certificates/contracts/{id}", Name = "GetContract")]
    public async Task<ActionResult<Contract>> GetContract(
        [FromRoute] Guid id,
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        var meteringPointOwner = User.FindFirstValue("subject");

        var contract = await service.GetById(id, meteringPointOwner, cancellationToken);

        return contract == null
            ? NotFound()
            : Ok(Contract.CreateFrom(contract));
    }

    /// <summary>
    /// Returns all the user's contracts for issuing granular certificates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ContractList), 200)]
    [ProducesResponseType(204)]
    [Route("api/certificates/contracts")]
    public async Task<ActionResult<ContractList>> GetAllContracts(
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        var meteringPointOwner = User.FindFirstValue("subject");

        var contracts = await service.GetByOwner(meteringPointOwner, cancellationToken);

        return contracts.IsEmpty()
            ? NoContent()
            : Ok(new ContractList { Result = contracts.Select(Contract.CreateFrom) });
    }

    /// <summary>
    /// Ends the contract on a specific date
    /// </summary>
    [HttpPatch]
    [ProducesResponseType(typeof(void), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 403)]
    [Route("api/certificates/contracts")]
    public async Task<ActionResult> EndContract(
        [FromBody] EndContract endContract,
        [FromServices] IValidator<EndContract> validator,
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        var meteringPointOwner = User.FindFirstValue("subject");

        var validationResult = await validator.ValidateAsync(endContract, cancellationToken);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var result = await service.EndContract(meteringPointOwner!, endContract.ContractId,
            endContract.EndDate != null ? DateTimeOffset.FromUnixTimeSeconds((long)endContract.EndDate) : DateTimeOffset.Now,
            cancellationToken);

        return result switch
        {
            NonExistingContract => NotFound($"No contract with id {endContract.ContractId} found"),
            MeteringPointOwnerNoMatch => Forbid(),
            Ended => Ok(),
            _ => throw new NotImplementedException($"{result.GetType()} not handled by {nameof(ContractsController)}")
        };
    }
}
