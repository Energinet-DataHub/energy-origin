using System.Linq;
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
        var certificateListProj = await querySession.LoadAsync<CertificateListProj>("ab20f689-36c2-4b50-aac2-ce93490b8702");
        if (certificateListProj == null || certificateListProj.Certificates.IsEmpty())
            return NoContent();

        var certificates = certificateListProj.Certificates.Values
            .Select(c => new Certificate
            {
                GSRN = c.GSRN,
                DateFrom = c.DateFrom,
                DateTo = c.DateTo,
                Quantity = c.Quantity
            });

        return new CertificateList
        {
            Result = certificates
                .OrderByDescending(c => c.DateFrom)
                .ThenBy(c => c.GSRN)
                .ToArray()
        };
    }
}
