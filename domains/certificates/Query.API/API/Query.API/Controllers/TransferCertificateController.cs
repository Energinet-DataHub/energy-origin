using System.Security.Claims;
using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
using API.Query.API.ApiModels.Responses;
using Baseline;
using Contracts.Transfer;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class TransferCertificateController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(200)]
    [Route("api/certificates/production/transfer")]
    public async Task<IActionResult> TransferCertificate(
        [FromBody] TransferCertificate transferCertificate,
        [FromServices] IRequestClient<TransferProductionCertificateRequest> requestClient)
    {
        var request = new TransferProductionCertificateRequest(
            CurrentOwner: transferCertificate.CurrentOwner,
            NewOwner: transferCertificate.NewOwner,
            CertificateId: transferCertificate.CertificateId
        );

        var response = await requestClient.GetResponse<TransferProductionCertificateResponse>(request);
        
        return Ok(response.Message.Status);
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [Route("api/certificates/owner/uuid")]
    public Task<ActionResult<OwnerUuid>> GetUuid()
    {
        var ownerObject = new OwnerUuid()
        {
            UUID = User.FindFirstValue("subject")
        };

        return Task.FromResult<ActionResult<OwnerUuid>>(
            ownerObject.UUID.IsEmpty()
            ? NotFound()
            : Ok(ownerObject));
    }
}
