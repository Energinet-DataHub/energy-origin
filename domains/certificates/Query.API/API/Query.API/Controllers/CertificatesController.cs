using System.Threading.Tasks;
using API.Models;
using API.Query.API.Projections;
using Marten;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
public class CertificatesController : ControllerBase
{
    [HttpGet]
    [Route("certificates")]
    public async Task<ActionResult<CertificateList>> Get([FromServices] IQuerySession querySession)
    {
        var projection = await querySession.LoadAsync<CertificateListProj>("ab20f689-36c2-4b50-aac2-ce93490b8702");
        return projection != null ? projection.ToApiModel() : NoContent();
    }
}
