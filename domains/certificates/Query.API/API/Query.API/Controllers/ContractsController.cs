using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
using API.Query.API.ApiModels.Requests;
using API.Query.API.ApiModels.Responses;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
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
[ApiVersion(ApiVersions.Version20240423)]
public class ContractsController : ControllerBase
{
    /// <summary>
    /// Create a contract that activates granular certificate generation for a metering point
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(typeof(void), 409)]
    [ApiVersion(ApiVersions.Version20230101)]
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

        var result = await service.Create(
            new CreateContracts([createContract]),
            user.Subject, user.Id, user.Name, user.Organization!.Name, user.Organization.Tin,
            cancellationToken);

        return result switch
        {
            GsrnNotFound => ValidationProblem(),
            ContractAlreadyExists => ValidationProblem(statusCode: 409),
            CreateContractResult.Success(var createdContract) => CreatedAtRoute("GetContract",
                new { id = createdContract[0].Id }, Contract.CreateFrom(createdContract[0])),
            _ => throw new NotImplementedException($"{result.GetType()} not handled by {nameof(ContractsController)}")
        };
    }

    /// <summary>
    /// Create contracts that activates granular certificate generation for a metering point
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(typeof(void), 409)]
    [ApiVersion(ApiVersions.Version20240423)]
    [Route("api/certificates/contracts")]
    public async Task<ActionResult> CreateContract(
        [FromBody] CreateContracts createContracts,
        [FromServices] IValidator<CreateContract> validator,
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        var user = new UserDescriptor(User);

        foreach (var createContract in createContracts.Contracts)
        {
            var validationResult = await validator.ValidateAsync(createContract, cancellationToken);
            if (!validationResult.IsValid)
            {
                validationResult.AddToModelState(ModelState, null);
                return ValidationProblem(ModelState);
            }
        }

        var result = await service.Create(
            createContracts,
            user.Subject, user.Id, user.Name, user.Organization!.Name, user.Organization.Tin,
            cancellationToken);

        return result switch
        {
            GsrnNotFound => ValidationProblem(),
            ContractAlreadyExists => ValidationProblem(statusCode: 409),
            CreateContractResult.Success(var createdContracts) => Created("",
                new ContractList { Result = createdContracts.Select(Contract.CreateFrom).ToList() }),
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
        var meteringPointOwner = user.Subject;

        var contract = await service.GetById(id, new List<Guid> { meteringPointOwner }, cancellationToken);

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
        var meteringPointOwner = user.Subject;

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
    [ApiVersion(ApiVersions.Version20230101)]
    [Route("api/certificates/contracts/{id}")]
    public async Task<ActionResult> UpdateEndDate(
        [FromRoute] Guid id,
        [FromBody] EditContractEndDate20230101 editContractEndDate,
        [FromServices] IValidator<EditContractEndDate20230101> validator,
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

        var result = await service.SetEndDate(
            new EditContracts([new EditContractEndDate
            {
                EndDate = editContractEndDate.EndDate,
                Id = id
            }]),
            user.Subject, user.Id, user.Name, user.Organization!.Name, user.Organization.Tin,
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
    [ApiVersion(ApiVersions.Version20240423)]
    [Route("api/certificates/contracts")]
    public async Task<ActionResult> UpdateEndDate(
        [FromBody] EditContracts editContracts,
        [FromServices] IValidator<EditContractEndDate> validator,
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        var user = new UserDescriptor(User);

        foreach (var contract in editContracts.Contracts)
        {
            var validationResult = await validator.ValidateAsync(contract, cancellationToken);
            if (!validationResult.IsValid)
            {
                validationResult.AddToModelState(ModelState, null);
                return ValidationProblem(ModelState);
            }
        }

        var result = await service.SetEndDate(
            editContracts,
            user.Subject, user.Id, user.Name, user.Organization!.Name, user.Organization.Tin,
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
