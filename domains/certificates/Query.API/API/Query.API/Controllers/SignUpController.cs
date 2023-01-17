using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
using API.Query.API.Clients;
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
    public async Task<ActionResult> SignUp([FromServices] IQuerySession querySession, [FromBody] CreateSignup createSignup, [FromServices] IValidator<CreateSignup> validator, [FromServices] IMeteringPointsClient client, CancellationToken cancellationToken)
    {
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
        // Save

        return Ok();
    }
}
