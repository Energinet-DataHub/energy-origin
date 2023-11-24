using System;
using System.Linq;
using System.Threading.Tasks;
using API.Shared.Extensions;
using API.Transfer.Api.Repository;
using API.Transfer.Api.v2023_01_01.Dto.Responses;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Transfer.Api.vX.Controllers;

[Authorize]
[ApiController]
[ApiVersion("20230101")]
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
