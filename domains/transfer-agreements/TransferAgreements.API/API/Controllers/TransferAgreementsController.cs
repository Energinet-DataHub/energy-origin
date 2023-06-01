using System;
using System.Security.Claims;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TransferAgreementsController : ControllerBase
{
    private readonly ITransferAgreementService transferAgreementService;

    public TransferAgreementsController(ITransferAgreementService transferAgreementService) => this.transferAgreementService = transferAgreementService;

    [HttpPost("api/transfer-agreements")]
    public async Task<ActionResult> Create([FromBody] CreateTransferAgreement request)
    {
        var transferAgreement = new TransferAgreement
        {
            SenderId = Guid.Parse(User.FindFirstValue("sub")),
            ActorId = User.FindFirstValue("atr"),
            StartDate = request.StartDate.UtcDateTime,
            EndDate = request.EndDate.UtcDateTime,
            ReceiverTin = request.ReceiverTin
        };

        var result = await transferAgreementService.CreateTransferAgreement(transferAgreement);

        return Ok(result);
    }
}
