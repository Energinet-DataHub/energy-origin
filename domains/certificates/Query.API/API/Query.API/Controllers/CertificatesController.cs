using System;
using System.Numerics;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Query.API.ApiModels;
using API.Query.API.Projections;
using CertificateEvents;
using CertificateEvents.Primitives;
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
    [Route("createcert")]
    public async Task<IActionResult> Get2([FromServices] IDocumentSession session)
    {
        var meteringPointOwner = User.FindFirstValue("subject");
        var certificateId = Guid.NewGuid();

        var createdEvent = new ProductionCertificateCreated(
            CertificateId: certificateId,
            GridArea: "GridArea",
            Period: new Period(DateTimeOffset.Now.ToUnixTimeSeconds(), DateTimeOffset.Now.AddHours(1).ToUnixTimeSeconds()),
            Technology: new Technology("foo", "BBbr"),
            MeteringPointOwner: meteringPointOwner,
            ShieldedGSRN: new ShieldedValue<string>("GSRN", BigInteger.Zero),
            ShieldedQuantity: new ShieldedValue<long>(42, BigInteger.Zero));

        var issuedEvent = new ProductionCertificateIssued(
            CertificateId: createdEvent.CertificateId,
            MeteringPointOwner: createdEvent.MeteringPointOwner,
            GSRN: createdEvent.ShieldedGSRN.Value);

        session.Events.StartStream(certificateId, createdEvent, issuedEvent);
        await session.SaveChangesAsync();

        return Ok();
    }
}
