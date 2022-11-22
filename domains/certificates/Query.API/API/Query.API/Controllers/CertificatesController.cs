using System.Security.Claims;
using System.Threading.Tasks;
using API.Query.API.ApiModels;
using API.Query.API.Projections;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class CertificatesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CertificateList), 200)]
    [ProducesResponseType(204)]
    [Route("certificates")]
    public async Task<ActionResult<CertificateList>> Get([FromServices] IQuerySession querySession, [FromServices] ILogger<CertificatesController> logger)
    {
        var meteringPointOwner = User.FindFirstValue("subject");
        logger.LogInformation("subject: {subject}", meteringPointOwner);
        var projection = await querySession.LoadAsync<CertificatesByOwnerView>(meteringPointOwner);
        return projection != null ? projection.ToApiModel() : NoContent();
    }
}
