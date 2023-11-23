using System;
using System.Linq;
using System.Threading.Tasks;
using API.Shared.Extensions;
using API.Transfer.Api.Repository;
using API.Transfer.Api.v2023_11_23.Dto.Responses;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Transfer.Api.v2023_11_23.Controllers;

[Authorize]
[ApiController]
[ApiVersion("20231123")]
[Route("api/transfer-agreements/history")]
public class TransferAgreementHistoryEntriesController : ControllerBase
{
    private readonly ITransferAgreementHistoryEntryRepository historyEntryRepository;

    public TransferAgreementHistoryEntriesController(ITransferAgreementHistoryEntryRepository historyEntryRepository) => this.historyEntryRepository = historyEntryRepository;

    [ProducesResponseType(typeof(TransferAgreementHistoryEntriesResponse), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{transferAgreementId}")]
    public async Task<ActionResult> GetHistoryEntriesForTransferAgreement(Guid transferAgreementId, [FromQuery] int offset, [FromQuery] int limit)
    {
        var subject = User.FindSubjectGuidClaim();
        var tin = User.FindSubjectTinClaim();
        var histories = await historyEntryRepository.GetHistoryEntriesForTransferAgreementPaginated(transferAgreementId, subject, tin, offset, limit);

        if (!histories.items.Any())
        {
            return NotFound();
        }

        var listResponse = new TransferAgreementHistoryEntriesResponse(histories.totalCount,
            histories.items.Select(history => history.ToDto(subject))
                .ToList());

        return Ok(listResponse);
    }
}
