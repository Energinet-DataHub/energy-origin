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

[Authorize]
[ApiController]
public class SignUpController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(302)]
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
        var document = await documentStoreHandler.GetByGsrn(long.Parse(createSignup.GSRN)); //TODO: Should be string

        if (document != null)
        {
            return Conflict();
        }

        // Save
        var userObject = new MeteringPointSignup()
        {
            Id = new Guid(),
            GSRN = long.Parse(createSignup.GSRN), //TODO: Should be string
            MeteringPointType = MeteringPointType.Production, // This needs to change, when we have data from datasync
            MeteringPointOwner = meteringPointOwner,
            SignupStartDate = DateTimeOffset.UtcNow, // Also needs change
            Created = DateTimeOffset.UtcNow
        };
        await documentStoreHandler.Save(userObject);

        return StatusCode(201);
    }
}
