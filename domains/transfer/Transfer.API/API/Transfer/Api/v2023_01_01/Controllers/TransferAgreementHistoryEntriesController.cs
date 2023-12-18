using System;
using System.Linq;
using System.Threading.Tasks;
using API.Transfer.Api.Repository;
using API.Transfer.Api.v2023_01_01.Dto.Responses;
using Asp.Versioning;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Transfer.Api.v2023_01_01.Controllers;

[Authorize]
[ApiController]
[ApiVersion("20230101")]
[Route("api/history/transfer-agreements")]
public class TransferAgreementHistoryEntriesController : ControllerBase
{
    private readonly ITransferAgreementHistoryEntryRepository historyEntryRepository;

    public TransferAgreementHistoryEntriesController(ITransferAgreementHistoryEntryRepository historyEntryRepository) => this.historyEntryRepository = historyEntryRepository;

    [Authorize(Policy = PolicyName.RequiresCompany)]
    [ProducesResponseType(typeof(TransferAgreementHistoryEntriesResponse), 200)]
    [ProducesResponseType(204)]
    [HttpGet("{transferAgreementId}")]
    public async Task<ActionResult> GetHistoryEntriesForTransferAgreement(Guid transferAgreementId)
    {
        var user = new UserDescriptor(HttpContext.User);

        var histories = await historyEntryRepository.GetHistoryEntriesForTransferAgreement(
            transferAgreementId,
            user.Subject.ToString(),
            user.Organization!.Tin
            );

        if (histories.Count == 0)
        {
            return NoContent();
        }

        var listResponse = new TransferAgreementHistoryEntriesResponse(
            histories.Select(history => history.ToDto(user.Subject.ToString()))
                .ToList());

        return Ok(listResponse);
    }
}
