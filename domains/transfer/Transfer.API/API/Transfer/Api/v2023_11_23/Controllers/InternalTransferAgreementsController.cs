using System.Linq;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using API.Transfer.Api.v2023_11_23.Dto.Responses;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Transfer.Api.v2023_11_23.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[ApiVersion("20231123")]
public class InternalTransferAgreementsController(ITransferAgreementRepository agreementRepository)
    : ControllerBase
{
    [HttpGet("api/internal-transfer-agreements/all")]
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
