using System.Linq;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using API.Transfer.Api.v2024_01_03.Dto.Responses;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Transfer.Api.v2024_01_03.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[ApiVersion("20240103")]
[ApiController]
public class InternalTransferAgreementsController(ITransferAgreementRepository agreementRepository)
    : ControllerBase
{
    [HttpGet("api/internal-transfer-agreements/all")]
    [AllowAnonymous]
    public async Task<ActionResult> GetAll()
    {
        var transferAgreements = await agreementRepository.GetAllTransferAgreements();

        return Ok(new InternalTransferAgreementsDto(transferAgreements.Select(ta => new InternalTransferAgreementDto(
            StartDate: ta.StartDate.ToUnixTimeSeconds(),
            EndDate: ta.EndDate?.ToUnixTimeSeconds(),
            SenderId: ta.SenderId.ToString(),
            ReceiverTin: ta.ReceiverTin,
            ReceiverReference: ta.ReceiverReference
        )).ToList()));
    }
}
