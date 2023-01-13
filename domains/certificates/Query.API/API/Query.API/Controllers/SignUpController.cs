using System;
using System.Security.Claims;
using System.Threading.Tasks;
using API.MasterDataService;
using API.Query.API.Repositories;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static API.Query.API.Repositories.IMeteringPointSignupRepository;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class SignUpController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(302)]
    [Route("api/signup")]
    public async Task<ActionResult> SignUp([FromServices] IDocumentSession session, [FromBody] CreateSignup createSignup)
    {
        var documentStoreHandler = new MeteringPointSignupRepository(session);
        var meteringPointOwner = User.FindFirstValue("subject");

        // Validate CreateSignup

        // Check ownership and if it is production type of GSRN in datahub

        // Check if GSRN is already signed up
        var document = documentStoreHandler.GetByGsrn(createSignup.Gsrn);

        if (!document.IsFaulted || document.Result != null)
        {
            return Conflict();
        }

        // Save
        var userObject = new MeteringPointSignup()
        {
            Id = new Guid(),
            GSRN = createSignup.Gsrn,
            MeteringPointType = MeteringPointType.Production, // This needs to change, when we have data from datasync
            MeteringPointOwner = meteringPointOwner,
            SignupStartDate = DateTimeOffset.UtcNow, // Also needs change
            Created = DateTimeOffset.UtcNow
        };
        var documentSaved = documentStoreHandler.Save(userObject);
        if (documentSaved.IsFaulted)
        {
            return Problem();
        }
        
        return Ok();
    }
}

public record CreateSignup(long Gsrn, long StartDate);
