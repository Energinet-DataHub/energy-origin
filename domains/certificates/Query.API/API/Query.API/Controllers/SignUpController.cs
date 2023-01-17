using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using API.CertificateGenerationSignupService;
using API.Query.API.ApiModels.Requests;
using API.Query.API.Repositories;
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
    [Route("api/signup")]
    public async Task<ActionResult> SignUp([FromBody] CreateSignup createSignup, [FromServices] IValidator<CreateSignup> validator, [FromServices] ICertificateGenerationSignupService service, CancellationToken cancellationToken)
    {
        var meteringPointOwner = User.FindFirstValue("subject");

        var validationResult = await validator.ValidateAsync(createSignup, cancellationToken);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var result = await service.Create(createSignup.GSRN, meteringPointOwner, DateTimeOffset.FromUnixTimeSeconds(createSignup.StartDate), cancellationToken);

        return result switch
        {
            GsrnNotFound => BadRequest($"GSRN {createSignup.GSRN} not found"),
            NotProductionMeteringPoint => BadRequest($"GSRN {createSignup.GSRN} is not a production metering point"),
            SignupAlreadyExists => Conflict(),
            Success => StatusCode(201),
            _ => throw new NotImplementedException($"{result.GetType()} not handled by {nameof(SignUpController)}")
        };
    }

    /// <summary>
    /// Returns all metering points signed up for granular certificate generation
    /// </summary>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(204)]
    [Route("api/signups")]
    public async Task<ActionResult> GetAllSignUps([FromServices] IDocumentSession session)
    {
        var documentStoreHandler = new MeteringPointSignupRepository(session);
        var meteringPointOwner = User.FindFirstValue("subject");

        var document = await documentStoreHandler.GetAllSignUps(meteringPointOwner);

        return document.IsEmpty()
            ? NoContent()
            : Ok(document);
    }
}
