using System;
using System.Linq;
using System.Threading.Tasks;
using API.ApiModels.Responses;
using API.Data;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/audits/transfer-agreements")]
public class TransferAgreementAuditsController : ControllerBase
{
    private readonly ITransferAgreementAuditRepository auditRepository;
    private readonly ITransferAgreementRepository transferAgreementRepository;

    public TransferAgreementAuditsController(ITransferAgreementAuditRepository auditRepository, ITransferAgreementRepository transferAgreementRepository)
    {
        this.auditRepository = auditRepository;
        this.transferAgreementRepository = transferAgreementRepository;
    }

    [ProducesResponseType(typeof(TransferAgreementAuditsResponse), 200)]
    [ProducesResponseType(204)]
    [HttpGet("{transferAgreementId}")]
    public async Task<ActionResult> GetAuditsForTransferAgreement(Guid transferAgreementId)
    {
        var subject = User.FindSubjectGuidClaim();
        var tin = User.FindSubjectTinClaim();
        var audits = await auditRepository.GetAuditsForTransferAgreement(transferAgreementId, subject, tin);

        if (!audits.Any())
        {
            return NoContent();
        }

        var transferAgreement = await transferAgreementRepository.GetTransferAgreement(transferAgreementId, subject, tin);

        if (transferAgreement == null)
        {
            return NotFound();
        }

        var listResponse = new TransferAgreementAuditsResponse(
            audits.Select(audit => audit.ToDto(ToTransferAgreementDto(transferAgreement: transferAgreement), subject))
                .ToList());

        return Ok(listResponse);
    }

    private static TransferAgreementDto ToTransferAgreementDto(TransferAgreement transferAgreement)
    {
        return new TransferAgreementDto(
            Id: transferAgreement.Id,
            StartDate: transferAgreement.StartDate.ToUnixTimeSeconds(),
            EndDate: transferAgreement.EndDate?.ToUnixTimeSeconds(),
            SenderName: transferAgreement.SenderName,
            SenderTin: transferAgreement.SenderTin,
            ReceiverTin: transferAgreement.ReceiverTin
        );
    }

}
