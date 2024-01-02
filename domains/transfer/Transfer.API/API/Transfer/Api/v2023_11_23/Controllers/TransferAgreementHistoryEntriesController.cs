using System;
using System.Linq;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using API.Transfer.Api.v2023_11_23.Dto.Responses;
using Asp.Versioning;
using DataContext.Models;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Transfer.Api.v2023_11_23.Controllers;

[Authorize]
[ApiController]
[ApiVersion("20231123")]
[Route("api/transfer-agreements")]
public class TransferAgreementHistoryEntriesController : ControllerBase
{
    private readonly ITransferAgreementHistoryEntryRepository historyEntryRepository;

    public TransferAgreementHistoryEntriesController(ITransferAgreementHistoryEntryRepository historyEntryRepository) => this.historyEntryRepository = historyEntryRepository;

    [Authorize(Policy = PolicyName.RequiresCompany)]
    [ProducesResponseType(typeof(TransferAgreementHistoryEntriesResponse), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{transferAgreementId}/history")]
    public async Task<ActionResult> GetHistoryEntriesForTransferAgreement(Guid transferAgreementId, [FromQuery] int offset, [FromQuery] int limit)
    {
        var user = new UserDescriptor(User);

        var histories = await historyEntryRepository.GetHistoryEntriesForTransferAgreement(
            transferAgreementId,
            user.Subject.ToString(),
            user.Organization!.Tin,
            new Pagination(offset, limit)
            );

        if (histories.items.Count == 0)
        {
            return NotFound();
        }

        var listResponse = new TransferAgreementHistoryEntriesResponse(histories.totalCount,
            histories.items.Select(history => history.ToDto(user.Subject.ToString()))
                .ToList());

        return Ok(listResponse);
    }
}
