using System.Linq;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using API.Transfer.Api.v2023_11_23.Dto.Responses;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Transfer.Api.v2023_11_23.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[ApiVersion("20231123")]
[ApiController]
[AllowAnonymous]
public class InternalTransferAgreementsController(ITransferAgreementRepository agreementRepository, ILogger<InternalTransferAgreementsController> logger)
    : ControllerBase
{
    [HttpGet("api/internal-transfer-agreements/all")]
    public async Task<ActionResult> GetAll()
    {
        logger.LogInformation("Getting all transfer agreements");
        var transferAgreements = await agreementRepository.GetAllTransferAgreements();
        logger.LogInformation("Found {count} transfer agreements", transferAgreements.Count);
        return Ok(new InternalTransferAgreementsDto(transferAgreements.Select(ta => new InternalTransferAgreementDto(
            StartDate: ta.StartDate.ToUnixTimeSeconds(),
            EndDate: ta.EndDate?.ToUnixTimeSeconds(),
            SenderId: ta.SenderId.ToString(),
            ReceiverTin: ta.ReceiverTin,
            ReceiverReference: ta.ReceiverReference
        )).ToList()));
    }
}
