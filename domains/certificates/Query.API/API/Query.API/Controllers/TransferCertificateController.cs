using System.Security.Claims;
using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
using API.Query.API.ApiModels.Responses;
using Baseline;
using Contracts.Transfer;
using FluentValidation;
using FluentValidation.AspNetCore;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Contracts.Transfer.TransferProductionCertificateResponse;

namespace API.Query.API.Controllers;

[Authorize]
[ApiController]
public class TransferCertificateController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [Route("api/certificates/production/transfer")]
    public async Task<IActionResult> TransferCertificate(
        [FromBody] TransferCertificate transferCertificate,
        [FromServices] IRequestClient<TransferProductionCertificateRequest> requestClient,
        [FromServices] IValidator<TransferCertificate> validator)
    {
        var validationResult = await validator.ValidateAsync(transferCertificate);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var request = new TransferProductionCertificateRequest(
            CurrentOwner: transferCertificate.CurrentOwner,
            NewOwner: transferCertificate.NewOwner,
            CertificateId: transferCertificate.CertificateId
        );

        var response = await requestClient.GetResponse<Success, Failure>(request);


        return Ok(
            new TransferProductionCertificateResponse(
                Status: response.Message.
                )
            );
/*
        if (response.Is(out Response<TransferProductionCertificateResponse>? success))
        {

        if (response.Is(out Response<Success>? _))

            return Ok();

        if (response.Is(out Response<Failure>? failure))
            return BadRequest(failure!.Message.Reason);

        return Conflict(); //TODO
        */
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
