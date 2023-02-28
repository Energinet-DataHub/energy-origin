using System;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
using API.Query.API.ApiModels.Responses;
using API.TransferCertificateService;
using Baseline;
using CertificateEvents;
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
        [FromServices] ITransferCertificateService service
        )
    {
        var response = await service.Get(transferCertificate);

        return Ok(response.Status);
    }


    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [Route("api/certificates/owner/uuid")]
    public Task<ActionResult<OwnerUuid>> GetUuid()
    {
        var ownerObject = new OwnerUuid()
        {
            UUID = User.FindFirstValue("subject ")
        };

        return Task.FromResult<ActionResult<OwnerUuid>>(
            ownerObject.UUID.IsEmpty()
            ? NotFound()
            : Ok(ownerObject));
    }

}
