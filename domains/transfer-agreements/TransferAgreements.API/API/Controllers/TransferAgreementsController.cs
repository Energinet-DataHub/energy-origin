using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.Data;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/transfer-agreements")]
public class TransferAgreementsController : ControllerBase
{
    private readonly ITransferAgreementRepository transferAgreementRepository;

    public TransferAgreementsController(ITransferAgreementRepository transferAgreementRepository) => this.transferAgreementRepository = transferAgreementRepository;

    [ProducesResponseType(201)]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateTransferAgreement request)
    {
        var actor = User.FindActorGuidClaim();
        var subject = User.FindSubjectGuidClaim();
        var subjectName = User.FindSubjectNameClaim();
        var subjectTin = User.FindSubjectTinClaim();

        var transferAgreement = new TransferAgreement
        {
            StartDate = DateTimeOffset.FromUnixTimeSeconds(request.StartDate),
            EndDate = DateTimeOffset.FromUnixTimeSeconds(request.EndDate),
            ActorId = actor,
            SenderId = Guid.Parse(subject),
            SenderName = subjectName,
            SenderTin = subjectTin,
            ReceiverTin = request.ReceiverTin
        };

        var result = await transferAgreementRepository.AddTransferAgreementToDb(transferAgreement);

        return CreatedAtAction(nameof(Get), new { id = result.Id }, ToTransferAgreementDto(result));
    }

    [ProducesResponseType(typeof(TransferAgreement), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{id}")]
    public async Task<ActionResult> Get([FromRoute] Guid id)
    {
        var tin = User.FindSubjectTinClaim()!;
        var subject = User.FindSubjectGuidClaim();

        var result = await transferAgreementRepository.GetTransferAgreement(id, subject, tin);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(ToTransferAgreementDto(result));
    }

    [ProducesResponseType(typeof(TransferAgreementsResponse), 200)]
    [ProducesResponseType(204)]
    [HttpGet]
    public async Task<ActionResult<TransferAgreementsResponse>> GetTransferAgreements()
    {
        var subject = User.FindSubjectGuidClaim();
        var userTin = User.FindSubjectTinClaim();

        var transferAgreements = await transferAgreementRepository.GetTransferAgreementsList(Guid.Parse(subject), userTin);

        if (!transferAgreements.Any())
        {
            return NoContent();
        }

        var listResponse = transferAgreements.Select(ToTransferAgreementDto)
            .ToList();

        return Ok(new TransferAgreementsResponse(listResponse));
    }

    private static TransferAgreementDto ToTransferAgreementDto(TransferAgreement transferAgreement)
    {
        return new TransferAgreementDto(
            Id: transferAgreement.Id,
            StartDate: transferAgreement.StartDate.ToUnixTimeSeconds(),
            EndDate: transferAgreement.EndDate.ToUnixTimeSeconds(),
            SenderName: transferAgreement.SenderName,
            SenderTin: transferAgreement.SenderTin,
            ReceiverTin: transferAgreement.ReceiverTin
        );
    }
}
