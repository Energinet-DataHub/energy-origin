using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using API.CertificateGenerationSignupServiceBla;
using API.Query.API.ApiModels.Requests;
using API.Query.API.ApiModels.Responses;
using FluentValidation;
using FluentValidation.AspNetCore;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static API.CertificateGenerationSignupServiceBla.CreateSignupResult;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class SignUpsController : ControllerBase
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
        [FromBody] CreateSignup createSignup,
        [FromServices] IValidator<CreateSignup> validator,
        [FromServices] ICertificateGenerationSignUpService service,
        CancellationToken cancellationToken)
    {
        var meteringPointOwner = User.FindFirstValue("subject");

        var validationResult = await validator.ValidateAsync(createSignup, cancellationToken);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var result = await service.Create(createSignup.GSRN, meteringPointOwner,
            DateTimeOffset.FromUnixTimeSeconds(createSignup.StartDate), cancellationToken);

        return result switch
        {
            GsrnNotFound => BadRequest($"GSRN {createSignup.GSRN} not found"),
            NotProductionMeteringPoint => BadRequest($"GSRN {createSignup.GSRN} is not a production metering point"),
            SignupAlreadyExists => Conflict(),
            Success(var createdSignup) => CreatedAtRoute(
                "GetSignUp",
                new { id = createdSignup.Id },
                SignUp.CreateFrom(createdSignup)),
            _ => throw new NotImplementedException($"{result.GetType()} not handled by {nameof(SignUpsController)}")
        };
    }

    /// <summary>
    /// Returns all metering points signed up for granular certificate generation
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SignUpList), 200)]
    [ProducesResponseType(204)]
    [Route("api/signUps")]
    public async Task<ActionResult<SignUpList>> GetAllSignUps(
        [FromServices] ICertificateGenerationSignUpService service,
        CancellationToken cancellationToken)
    {
        var meteringPointOwner = User.FindFirstValue("subject");

        var signUps = await service.GetByOwner(meteringPointOwner, cancellationToken);

        return signUps.IsEmpty()
            ? NoContent()
            : Ok(new SignUpList { Result = signUps.Select(SignUp.CreateFrom) });
    }

    /// <summary>
    /// Returns sign up based on the id
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SignUp), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [Route("api/signUps/{id}", Name = "GetSignUp")]
    public async Task<ActionResult<SignUp>> GetSignUp(
        [FromRoute] Guid id,
        [FromServices] ICertificateGenerationSignUpService service,
        CancellationToken cancellationToken)
    {
        var meteringPointOwner = User.FindFirstValue("subject");

        var signUp = await service.GetById(id, meteringPointOwner, cancellationToken);

        return signUp == null
            ? NotFound()
            : Ok(SignUp.CreateFrom(signUp));
    }
}
