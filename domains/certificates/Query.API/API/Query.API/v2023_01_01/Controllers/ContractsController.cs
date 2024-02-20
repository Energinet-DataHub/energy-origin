using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
using API.Query.API.v2023_01_01.ApiModels.Requests;
using API.Query.API.v2023_01_01.ApiModels.Responses;
using Asp.Versioning;
using DataContext.Models;
using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.TokenValidation.Utilities;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static API.ContractService.CreateContractResult;
using static API.ContractService.SetEndDateResult;

namespace API.Query.API.v2023_01_01.Controllers;

[Authorize]
[ApiController]
[ApiVersion("20230101")]
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
        [FromServices] IActivityLogEntryRepository activityLogEntryRepository,
        CancellationToken cancellationToken)
    {
        var user = new UserDescriptor(User);
        var meteringPointOwner = user.Subject.ToString();

        var validationResult = await validator.ValidateAsync(createContract, cancellationToken);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var startDate = DateTimeOffset.FromUnixTimeSeconds(createContract.StartDate);
        DateTimeOffset? endDate = createContract.EndDate.HasValue ? DateTimeOffset.FromUnixTimeSeconds(createContract.EndDate.Value) : null;

        var result = await service.Create(
            createContract.GSRN,
            meteringPointOwner,
            startDate,
            endDate,
            cancellationToken);

        return result switch
        {
            GsrnNotFound => ValidationProblem($"GSRN {createContract.GSRN} not found"),
            ContractAlreadyExists => ValidationProblem(statusCode: 409),
            CreateContractResult.Success(var createdContract) => await LogCreatedAndReturnCreated(activityLogEntryRepository, user, createdContract),
            _ => throw new NotImplementedException($"{result.GetType()} not handled by {nameof(ContractsController)}")
        };
    }

    private async Task<CreatedAtRouteResult> LogCreatedAndReturnCreated(IActivityLogEntryRepository activityLogEntryRepository, UserDescriptor user,
        CertificateIssuingContract createdContract)
    {
        await activityLogEntryRepository.AddActivityLogEntryAsync(ActivityLogEntry.Create(user.Subject, ActivityLogEntry.ActorTypeEnum.User,
            user.Name, user.Organization!.Tin, user.Organization.Name, ActivityLogEntry.EntityTypeEnum.MeteringPoint,
            ActivityLogEntry.ActionTypeEnum.Activated, createdContract.GSRN));

        return CreatedAtRoute(
            "GetContract",
            new { id = createdContract.Id },
            Contract.CreateFrom(createdContract));
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
        [FromServices] IActivityLogEntryRepository activityLogEntryRepository,
        CancellationToken cancellationToken)
    {
        var user = new UserDescriptor(User);
        var meteringPointOwner = user.Subject.ToString();

        var validationResult = await validator.ValidateAsync(editContractEndDate, cancellationToken);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        DateTimeOffset? newEndDate = editContractEndDate.EndDate.HasValue ? DateTimeOffset.FromUnixTimeSeconds(editContractEndDate.EndDate.Value) : null;

        var result = await service.SetEndDate(
            id,
            meteringPointOwner,
            newEndDate,
            cancellationToken);

        var contract = await service.GetById(id, meteringPointOwner, cancellationToken);

        return result switch
        {
            NonExistingContract => NotFound(),
            MeteringPointOwnerNoMatch => Forbid(),
            EndDateBeforeStartDate => ValidationProblem("EndDate must be after StartDate"),
            SetEndDateResult.Success => await LogChangedEndDateAndReturnOk(activityLogEntryRepository, user, contract!.GSRN),
            _ => throw new NotImplementedException($"{result.GetType()} not handled by {nameof(ContractsController)}")
        };
    }

    private async Task<OkResult> LogChangedEndDateAndReturnOk(IActivityLogEntryRepository activityLogEntryRepository, UserDescriptor user,
        string gsrn)
    {
        await activityLogEntryRepository.AddActivityLogEntryAsync(ActivityLogEntry.Create(user.Subject, ActivityLogEntry.ActorTypeEnum.User,
            user.Name, user.Organization!.Tin, user.Organization.Name, ActivityLogEntry.EntityTypeEnum.MeteringPoint,
            ActivityLogEntry.ActionTypeEnum.EndDateChanged, gsrn));

        return Ok();
    }
}
