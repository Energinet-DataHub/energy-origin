using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.ApiModels.Responses;
using API.Data;
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

    public TransferAgreementAuditsController(ITransferAgreementAuditRepository auditRepository) => this.auditRepository = auditRepository;


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

        var listResponse = new TransferAgreementAuditsResponse(
            audits.Select(audit => audit.ToDto(subject))
                .ToList());

        return Ok(listResponse);
    }
}
