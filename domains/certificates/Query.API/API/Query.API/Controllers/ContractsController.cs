using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.ContractService;
using API.Query.API.ApiModels.Requests;
using API.Query.API.ApiModels.Responses;
using Asp.Versioning;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static API.ContractService.CreateContractResult;
using static API.ContractService.SetEndDateResult;

namespace API.Query.API.Controllers;

[Authorize(Policy = Policy.FrontendOr3rdParty)]
[ApiController]
[ApiVersion(ApiVersions.Version1)]
[Produces("application/json")]
public class ContractsController(IdentityDescriptor identityDescriptor, AccessDescriptor accessDescriptor) : ControllerBase
{
    /// <summary>
    /// Create contracts that activates granular certificate generation for a metering point
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(typeof(void), 409)]
    [Route("api/certificates/contracts")]
    public async Task<ActionResult> CreateContract(
        [FromBody] CreateContracts createContracts,
        [Required][FromQuery] Guid organizationId,
        [FromServices] IValidator<CreateContract> validator,
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        if (!accessDescriptor.IsAuthorizedToOrganization(organizationId))
        {
            return Forbid();
        }

        foreach (var createContract in createContracts.Contracts)
        {
            var validationResult = await validator.ValidateAsync(createContract, cancellationToken);
            if (!validationResult.IsValid)
            {
                validationResult.AddToModelState(ModelState, null);
                return ValidationProblem(ModelState);
            }
        }
        var isTrial = identityDescriptor.IsTrial();

        var result = await service.Create(createContracts, organizationId, identityDescriptor.Subject, identityDescriptor.Name,
            identityDescriptor.OrganizationName, identityDescriptor.OrganizationCvr ?? string.Empty, isTrial, cancellationToken);

        return result switch
        {
            GsrnNotFound(var gsrn) => ValidationProblem(detail: $"GSRN {gsrn} was not found"),
            CannotBeUsedForIssuingCertificates(var gsrn) => ValidationProblem(statusCode: 409, detail: $"GSRN {gsrn} cannot be used for issuing certificates"),
            ContractAlreadyExists(var existing) => ValidationProblem(statusCode: 409, detail: $"{existing?.GSRN} already has an active contract"),
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
    [Route("api/certificates/contracts/{id}")]
    public async Task<ActionResult<Contract>> GetContract(
        [FromRoute] Guid id,
        [Required][FromQuery] Guid organizationId,
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        if (!accessDescriptor.IsAuthorizedToOrganization(organizationId))
        {
            return Forbid();
        }

        var contract = await service.GetById(id, identityDescriptor.AuthorizedOrganizationIds.Concat([identityDescriptor.OrganizationId]).ToList(),
            cancellationToken);

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
        [Required][FromQuery] Guid organizationId,
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        if (!accessDescriptor.IsAuthorizedToOrganization(organizationId))
        {
            return Forbid();
        }

        var contracts = await service.GetByOwner(organizationId, cancellationToken);

        return contracts.Any()
            ? Ok(new ContractList { Result = contracts.Select(Contract.CreateFrom) })
            : Ok(new ContractList());
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
        [FromBody] EditContracts editContracts,
        [Required][FromQuery] Guid organizationId,
        [FromServices] IValidator<EditContractEndDate> validator,
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        if (!accessDescriptor.IsAuthorizedToOrganization(organizationId))
        {
            return Forbid();
        }

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
            organizationId,
            identityDescriptor.Subject,
            identityDescriptor.Name,
            identityDescriptor.OrganizationName,
            identityDescriptor.OrganizationCvr ?? string.Empty,
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
