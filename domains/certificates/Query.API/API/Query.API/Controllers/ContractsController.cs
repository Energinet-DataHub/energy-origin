using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
using API.Query.API.ApiModels.Requests;
using API.Query.API.ApiModels.Responses;
using API.UnitOfWork;
using Asp.Versioning;
using DataContext.ValueObjects;
using EnergyOrigin.TokenValidation.Utilities;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static API.ContractService.CreateContractResult;
using static API.ContractService.SetEndDateResult;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.Version20230101)]
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
        var user = new UserDescriptor(User);

        var validationResult = await validator.ValidateAsync(createContract, cancellationToken);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var startDate = DateTimeOffset.FromUnixTimeSeconds(createContract.StartDate);
        DateTimeOffset? endDate = createContract.EndDate.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(createContract.EndDate.Value)
            : null;

        var result = await service.Create(
            createContract.GSRN,
            user,
            startDate,
            endDate,
            cancellationToken);

        return result switch
        {
            GsrnNotFound => ValidationProblem($"GSRN {createContract.GSRN} not found"),
            ContractAlreadyExists => ValidationProblem(statusCode: 409),
            CreateContractResult.Success(var createdContract) => CreatedAtRoute("GetContract",
                new { id = createdContract.Id }, Contract.CreateFrom(createdContract)),
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
        var user = new UserDescriptor(User);
        var meteringPointOwner = user.Subject.ToString();

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
    [Route("api/certificates/contracts")]
    public async Task<ActionResult<ContractList>> GetAllContracts(
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        var user = new UserDescriptor(User);
        var meteringPointOwner = user.Subject.ToString();

        var contracts = await service.GetByOwner(meteringPointOwner, cancellationToken);

        return contracts.Any()
            ? Ok(new ContractList { Result = contracts.Select(Contract.CreateFrom) })
            : Ok(new ContractList());
    }

    /// <summary>
    /// Edit the end date for contract
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(void), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 403)]
    [Route("api/certificates/contracts/{id}")]
    public async Task<ActionResult> UpdateEndDate(
        [FromRoute] Guid id,
        [FromBody] EditContractEndDate editContractEndDate,
        [FromServices] IValidator<EditContractEndDate> validator,
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        var user = new UserDescriptor(User);

        var validationResult = await validator.ValidateAsync(editContractEndDate, cancellationToken);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var newEndDate = editContractEndDate.EndDate.HasValue
            ? UnixTimestamp.Create(editContractEndDate.EndDate.Value)
            : null;
        var contracts = new List<(Guid id, string gsrn, UnixTimestamp? newEndDate)> { (id, editContractEndDate.Gsrn, newEndDate) };
        var result = await service.SetEndDate(
            contracts,
            user,
            cancellationToken);

        return result switch
        {
            NonExistingContract => NotFound(),
            MeteringPointOwnerNoMatch => Forbid(),
            EndDateBeforeStartDate => ValidationProblem("EndDate must be after StartDate"),
            OverlappingContract => ValidationProblem(statusCode: 409),
            SetEndDateResult.Success => Ok(),
            _ => throw new NotImplementedException($"{result.GetType()} not handled by {nameof(ContractsController)}")
        };
    }

    /// <summary>
    /// Edit the end date for multiple contracts
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(void), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 403)]
    [Route("api/certificates/contracts")]
    public async Task<ActionResult> UpdateEndDate(
        [FromBody] MultipleEditContract multipleEditContract,
        [FromServices] IValidator<EditContractEndDate> validator,
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        var user = new UserDescriptor(User);
        var updatedContracts = new List<(Guid id, string gsrn, UnixTimestamp? endDate)>();

        foreach (var contract in multipleEditContract.Contracts)
        {
            var validationResult = await validator.ValidateAsync(contract, cancellationToken);
            if (!validationResult.IsValid)
            {
                validationResult.AddToModelState(ModelState, null);
                return ValidationProblem(ModelState);
            }

            var newEndDate = contract.EndDate.HasValue
                ? UnixTimestamp.Create(contract.EndDate.Value)
                : null;
            updatedContracts.Add((contract.Id!.Value, contract.Gsrn, newEndDate));
        }

        var result = await service.SetEndDate(
            updatedContracts,
            user,
            cancellationToken);

        return result switch
        {
            NonExistingContract => NotFound(),
            MeteringPointOwnerNoMatch => Forbid(),
            EndDateBeforeStartDate => ValidationProblem("EndDate must be after StartDate"),
            OverlappingContract => ValidationProblem(statusCode: 409),
            SetEndDateResult.Success => Ok(),
            _ => throw new NotImplementedException($"{result.GetType()} not handled by {nameof(ContractsController)}")
        };
    }
}
