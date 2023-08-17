using System;
using System.Linq;
using System.Threading.Tasks;
using API.Query.API.ApiModels.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectOrigin.WalletSystem.V1;
using ProjectOriginClients;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class CertificatesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CertificateList), 200)]
    [ProducesResponseType(204)]
    [Route("api/certificates")]
    public async Task<ActionResult<CertificateList>> Get([FromServices] WalletService.WalletServiceClient client)
    {
        var certificates = await client.QueryGranularCertificatesAsync(new QueryRequest());

        return certificates.GranularCertificates.Any()
            ? Ok(new CertificateList
            {
                Result = certificates.GranularCertificates
                    .Select(Map)
                    .OrderByDescending(c => c.DateFrom)
                    .ThenBy(c => c.GSRN)
                    .ToArray()
            })
            : NoContent();
    }

    private static Certificate Map(GranularCertificate c) =>
        new()
        {
            Id = Guid.Parse(c.FederatedId.StreamId.Value),
            DateFrom = c.Start.ToDateTimeOffset().ToUnixTimeSeconds(),
            DateTo = c.End.ToDateTimeOffset().ToUnixTimeSeconds(),
            GSRN = c.Attributes.FirstOrDefault(a => a.Key == Registry.Attributes.AssetId)?.Value ?? "",
            FuelCode = c.Attributes.FirstOrDefault(a => a.Key == Registry.Attributes.FuelCode)?.Value ?? "",
            TechCode = c.Attributes.FirstOrDefault(a => a.Key == Registry.Attributes.TechCode)?.Value ?? "",
            GridArea = c.GridArea,
            Quantity = c.Quantity
        };
}
