using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using API.CertificateGenerationSignupService;
using API.CertificateGenerationSignupService.Repositories;
using API.Query.API.ApiModels.Requests;
using API.Query.API.ApiModels.Responses;
using FluentValidation;
using FluentValidation.AspNetCore;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static API.CertificateGenerationSignupService.CreateSignupResult;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class SignUpController : ControllerBase
{
    /// <summary>
    /// Signs up a metering point for granular certificate generation
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(void), 409)]
    [Route("api/signups")]
    public async Task<ActionResult> SignUp(
        [FromBody] CreateSignup createSignup,
        [FromServices] IValidator<CreateSignup> validator,
        [FromServices] ICertificateGenerationSignupService service,
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
                "GetSignUpDocument",
                new { id = createdSignup.Id },
                ApiModels.Responses.SignUp.CreateFrom(createdSignup)),
            _ => throw new NotImplementedException($"{result.GetType()} not handled by {nameof(SignUpController)}")
        };
    }

    /// <summary>
    /// Returns all metering points signed up for granular certificate generation
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListResult<SignUp>), 200)]
    [ProducesResponseType(204)]
    [Route("api/signups")]
    public async Task<ActionResult<ListResult<SignUp>>> GetAllSignUps([FromServices] IDocumentSession session)
    {
        var documentStoreHandler = new MeteringPointSignupRepository(session);
        var meteringPointOwner = User.FindFirstValue("subject");

        var signUps = await documentStoreHandler.GetAllMeteringPointOwnerSignUps(meteringPointOwner);

        return signUps.IsEmpty()
            ? NoContent()
            : Ok(new ListResult<SignUp> { Result = signUps.Select(ApiModels.Responses.SignUp.CreateFrom) });
    }


    /// <summary>
    /// Returns sign up based on the id
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SignUp), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [Route("api/signups/{id}", Name = "GetSignUpDocument")]
    public async Task<ActionResult<SignUp>> GetSignUpDocument(
        [FromRoute] Guid id,
        [FromServices] IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var meteringPointOwner = User.FindFirstValue("subject");
        var documentStoreHandler = new MeteringPointSignupRepository(session);
        var signUp = await documentStoreHandler.GetByDocumentId(id, cancellationToken);

        if (signUp == null)
            return NotFound();

        if (signUp.MeteringPointOwner.Trim() != meteringPointOwner.Trim())
            return NotFound();

        return Ok(ApiModels.Responses.SignUp.CreateFrom(signUp));
    }
}
