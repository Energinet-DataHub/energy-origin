using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Query.API.ApiModels;
using API.Query.API.Projections;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class CertificatesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CertificateList), 200)]
    [ProducesResponseType(204)]
    [Route("certificates")]
    public async Task<ActionResult<CertificateList>> Get([FromServices] IQuerySession querySession)
    {
        var meteringPointOwner = User.FindFirstValue("subject");
        var projection = await querySession.LoadAsync<CertificatesByOwnerView>(meteringPointOwner);
        return projection != null ? projection.ToApiModel() : NoContent();
    }

    [HttpGet]
    [ProducesResponseType(typeof(DateTimeOffset), 200)]
    [ProducesResponseType(204)]
    [Route("syncstate")]
    public async Task<ActionResult<DateTimeOffset>> GetSyncState([FromServices] IQuerySession querySession, [FromQuery] string gsrn)
    {
        var meteringPointOwner = User.FindFirstValue("subject");
        var projection = await querySession.LoadAsync<CertificatesByOwnerView>(meteringPointOwner);
        if (projection == null)
            return NoContent();

        var maxDateTo = projection.Certificates.Values
            .Where(c => gsrn.Equals(c.GSRN, StringComparison.InvariantCultureIgnoreCase))
            .Select(c => c.DateTo)
            .DefaultIfEmpty(0)
            .Max();

        if (maxDateTo > 0)
            return DateTimeOffset.FromUnixTimeSeconds(maxDateTo);
        
        return NoContent();
    }
}
