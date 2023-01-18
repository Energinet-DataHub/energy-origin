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

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class ContractsController : ControllerBase
{
    /// <summary>
    /// Signs up a metering point for granular certificate generation
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(void), 409)]
    [Route("api/signUps")]
    public async Task<ActionResult> CreateSignUp(
        [FromBody] CreateContract createContract,
        [FromServices] IValidator<CreateContract> validator,
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        var meteringPointOwner = User.FindFirstValue("subject");

        var validationResult = await validator.ValidateAsync(createContract, cancellationToken);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var result = await service.Create(createContract.GSRN, meteringPointOwner,
            DateTimeOffset.FromUnixTimeSeconds(createContract.StartDate), cancellationToken);

        return result switch
        {
            GsrnNotFound => BadRequest($"GSRN {createContract.GSRN} not found"),
            NotProductionMeteringPoint => BadRequest($"GSRN {createContract.GSRN} is not a production metering point"),
            ContractAlreadyExists => Conflict(),
            Success(var createdSignUp) => CreatedAtRoute(
                "GetSignUp",
                new { id = createdSignUp.Id },
                Contract.CreateFrom(createdSignUp)),
            _ => throw new NotImplementedException($"{result.GetType()} not handled by {nameof(ContractsController)}")
        };
    }

    /// <summary>
    /// Returns all metering points signed up for granular certificate generation
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ContractList), 200)]
    [ProducesResponseType(204)]
    [Route("api/signUps")]
    public async Task<ActionResult<ContractList>> GetAllSignUps(
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        var meteringPointOwner = User.FindFirstValue("subject");

        var signUps = await service.GetByOwner(meteringPointOwner, cancellationToken);

        return signUps.IsEmpty()
            ? NoContent()
            : Ok(new ContractList { Result = signUps.Select(Contract.CreateFrom) });
    }

    /// <summary>
    /// Returns sign up based on the id
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Contract), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [Route("api/signUps/{id}", Name = "GetSignUp")]
    public async Task<ActionResult<Contract>> GetSignUp(
        [FromRoute] Guid id,
        [FromServices] IContractService service,
        CancellationToken cancellationToken)
    {
        var meteringPointOwner = User.FindFirstValue("subject");

        var signUp = await service.GetById(id, meteringPointOwner, cancellationToken);

        return signUp == null
            ? NotFound()
            : Ok(Contract.CreateFrom(signUp));
    }
}
