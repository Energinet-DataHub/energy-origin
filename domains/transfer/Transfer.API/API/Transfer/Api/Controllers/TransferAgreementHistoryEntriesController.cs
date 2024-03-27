using System;
using System.Linq;
using System.Threading.Tasks;
using API.Transfer.Api.Dto.Responses;
using API.UnitOfWork;
using Asp.Versioning;
using DataContext.Models;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Transfer.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.Version20240103)]
[Route("api/transfer/transfer-agreements")]
public class TransferAgreementHistoryEntriesController(IUnitOfWork unitOfWork)
    : ControllerBase
{
    [Authorize(Policy = PolicyName.RequiresCompany)]
    [ProducesResponseType(typeof(TransferAgreementHistoryEntriesResponse), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{transferAgreementId}/history")]
    public async Task<ActionResult> GetHistoryEntriesForTransferAgreement(Guid transferAgreementId, [FromQuery] int offset, [FromQuery] int limit)
    {
        var user = new UserDescriptor(User);

        var histories = await unitOfWork.TransferAgreementHistoryEntryRepo.GetHistoryEntriesForTransferAgreement(
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
            histories.items.Select(history => TransferAgreementHistoryEntryMapper.ToDto(history, user.Subject.ToString()))
                .ToList());

        return Ok(listResponse);
    }
}
