using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
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
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [Route("api/certificates/transfer")]
    public async Task<IActionResult> TransferCertificate(
        [FromBody] TransferCertificate transferCertificate,
        [FromServices] IValidator<TransferCertificate> validator,
        [FromServices] IRequestClient<TransferProductionCertificateRequest> requestClient)
    {
        var validationResult = await validator.ValidateAsync(transferCertificate);
        if (!validationResult.IsValid)
        {
            validationResult.AddToModelState(ModelState, null);
            return ValidationProblem(ModelState);
        }

        var request = new TransferProductionCertificateRequest(
            transferCertificate.Source,
            transferCertificate.Target,
            transferCertificate.CertificateId);

        var response = await requestClient.GetResponse<Success, Failure>(request);

        if (response.Is(out Response<Success>? _))
            return Ok();

        var failureReason = ((Failure)response.Message).Reason;
        return BadRequest(failureReason);
    }
}
