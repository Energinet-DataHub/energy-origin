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
using System;
using FluentValidation;
using API.ContractService;
using FluentValidation.AspNetCore;
using static API.ContractService.CreateContractResult;
using static API.ContractService.SetEndDateResult;
using API.Query.API.ApiModels.Requests.Internal;
using API.Query.API.ApiModels.Responses.Internal;

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

    /// <summary>
    /// Create contracts that activates granular certificate generation for a metering point
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(typeof(void), 409)]
    public async Task<ActionResult> CreateContract(
        [FromBody] CreateContracts createContracts,
        [FromServices] IValidator<CreateContract> validator,
        [FromServices] IAdminPortalContractService service,
        CancellationToken cancellationToken)
    {
        foreach (var createContract in createContracts.Contracts)
        {
            var validationResult = await validator.ValidateAsync(createContract, cancellationToken);
            if (!validationResult.IsValid)
            {
                validationResult.AddToModelState(ModelState, null);
                return ValidationProblem(ModelState);
            }
        }

        var result = await service.Create(createContracts, cancellationToken);

        return result switch
        {
            GsrnNotFound(var gsrn) => ValidationProblem(detail: $"GSRN {gsrn} was not found"),
            CannotBeUsedForIssuingCertificates(var gsrn) => ValidationProblem(statusCode: 409, detail: $"GSRN {gsrn} cannot be used for issuing certificates"),
            ContractAlreadyExists(var existing) => ValidationProblem(statusCode: 409, detail: $"{existing?.GSRN} already has an active contract"),
            CreateContractResult.Success(var createdContracts) => Created("",
                new ContractList { Result = [.. createdContracts.Select(Contract.CreateFrom)] }),
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
    public async Task<ActionResult> UpdateEndDate(
        [FromBody] EditContracts editContracts,
        [FromServices] IValidator<EditContractEndDate> validator,
        [FromServices] IAdminPortalContractService service,
        CancellationToken cancellationToken)
    {
        foreach (var contract in editContracts.Contracts)
        {
            var validationResult = await validator.ValidateAsync(contract, cancellationToken);
            if (!validationResult.IsValid)
            {
                validationResult.AddToModelState(ModelState, null);
                return ValidationProblem(ModelState);
            }
        }

        var result = await service.SetEndDate(editContracts, cancellationToken);

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
