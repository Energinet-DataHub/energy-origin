using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using API.MasterDataService;
using API.Query.API.ApiModels.Requests;
using API.Query.API.Clients;
using API.Query.API.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Query.API.Controllers;

// /api/signups -> All signups
// api/signups/<guid>

[Authorize]
[ApiController]
public class SignUpController : ControllerBase
{
    /// <summary>
    /// Signs up a metering point for granular certificate generation.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(void), 409)]
    [Route("api/signup")]
    public async Task<ActionResult> SignUp([FromServices] IDocumentSession session, [FromBody] CreateSignup createSignup, [FromServices] IValidator<CreateSignup> validator, [FromServices] IMeteringPointsClient client, CancellationToken cancellationToken)
    {
        var documentStoreHandler = new MeteringPointSignupRepository(session);
        var meteringPointOwner = User.FindFirstValue("subject");

        // Validate CreateSignup
        var validationResult = await validator.ValidateAsync(createSignup, cancellationToken);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        // Check ownership and if it is production type of GSRN in datahub
        var meteringPoints = await client.GetMeteringPoints(meteringPointOwner, cancellationToken);
        var matchingMeteringPoint = meteringPoints?.MeteringPoints.FirstOrDefault(mp => mp.GSRN == createSignup.GSRN);
        if (matchingMeteringPoint == null)
            return BadRequest($"GSRN {createSignup.GSRN} not found");
        if (matchingMeteringPoint.Type != MeterType.Production)
            return BadRequest($"GSRN {createSignup.GSRN} is not a production metering point");

        // Check if GSRN is already signed up
        var document = await documentStoreHandler.GetByGsrn(createSignup.GSRN); //TODO: Should be string

        if (document != null)
        {
            return Conflict();
        }

        // Save
        var userObject = new MeteringPointSignup
        {
            Id = new Guid(),
            GSRN = createSignup.GSRN,
            MeteringPointType = MeteringPointType.Production,
            MeteringPointOwner = meteringPointOwner,
            SignupStartDate = DateTimeOffset.FromUnixTimeSeconds(createSignup.StartDate),
            Created = DateTimeOffset.UtcNow
        };
        await documentStoreHandler.Save(userObject);

        return StatusCode(201);
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [Route("api/signups")]
    public async Task<ActionResult> GetAllSignUps([FromServices] IDocumentSession session)
    {
        var documentStoreHandler = new MeteringPointSignupRepository(session);
        var meteringPointOwner = User.FindFirstValue("subject");

        var document = await documentStoreHandler.GetAllSignUps(meteringPointOwner);

        if (document.IsEmpty())
        {
            return NotFound();
        }

        return Ok(document);
    }


}
