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
[Route("api/history/transfer-agreements")]
public class TransferAgreementHistoryEntriesController : ControllerBase
{
    private readonly ITransferAgreementHistoryEntryRepository historyEntryRepository;

    public TransferAgreementHistoryEntriesController(ITransferAgreementHistoryEntryRepository historyEntryRepository) => this.historyEntryRepository = historyEntryRepository;


    [ProducesResponseType(typeof(TransferAgreementHistoryEntriesResponse), 200)]
    [ProducesResponseType(204)]
    [HttpGet("{transferAgreementId}")]
    public async Task<ActionResult> GetHistoryEntriesForTransferAgreement(Guid transferAgreementId)
    {
        var subject = User.FindSubjectGuidClaim();
        var tin = User.FindSubjectTinClaim();
        var histories = await historyEntryRepository.GetHistoryEntriesForTransferAgreement(transferAgreementId, subject, tin);

        if (!histories.Any())
        {
            return NoContent();
        }

        var listResponse = new TransferAgreementHistoryEntriesResponse(
            histories.Select(history => history.ToDto(subject))
                .ToList());

        return Ok(listResponse);
    }
}
