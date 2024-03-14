using System;
using System.Linq;
using System.Threading.Tasks;
using API.Transfer.Api.Dto.Responses;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Transfer.Application.Repositories;
using Transfer.Domain.Entities;

namespace API.Transfer.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.Version20240103)]
[Route("api/transfer/transfer-agreements")]
public class TransferAgreementHistoryEntriesController(ITransferAgreementHistoryEntryRepository historyEntryRepository)
    : ControllerBase
{
    [Authorize(Policy = PolicyName.RequiresCompany)]
    [ProducesResponseType(typeof(TransferAgreementHistoryEntriesResponse), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{transferAgreementId}/history")]
    public async Task<ActionResult> GetHistoryEntriesForTransferAgreement(Guid transferAgreementId, [FromQuery] int offset, [FromQuery] int limit)
    {
        var user = new UserDescriptor(User);

        var historyView = await historyEntryRepository.GetHistoryEntriesForTransferAgreement(
            transferAgreementId,
            user.Subject.ToString(),
            user.Organization!.Tin,
            new Pagination(offset, limit)
            );

        if (historyView.items.Count == 0)
        {
            return NotFound();
        }

        var listResponse = new TransferAgreementHistoryEntriesResponse(historyView.totalCount,
            historyView.items.Select(history => TransferAgreementHistoryEntryMapper.ToDto(history, user.Subject.ToString()))
                .ToList());

        return Ok(listResponse);
    }
}
