using System.Security.Claims;
using System.Threading.Tasks;
using API.Models;
using API.Query.API.Projections;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
public class CertificatesController : ControllerBase
{
    [HttpGet]
    [Route("certificates")]
    public async Task<ActionResult<CertificateList>> Get([FromServices] IQuerySession querySession)
    {
        var meteringPointOwner = User.FindFirstValue("subject");
        var projection = await querySession.LoadAsync<CertificatesByOwnerView>(meteringPointOwner);
        return projection != null ? projection.ToApiModel() : NoContent();
    }
}
