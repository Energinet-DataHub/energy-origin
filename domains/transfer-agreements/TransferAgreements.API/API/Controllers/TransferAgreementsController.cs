using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.Data;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
public class TransferAgreementsController : ControllerBase
{
    private readonly ITransferAgreementRepository transferAgreementRepository;

    public TransferAgreementsController(ITransferAgreementRepository transferAgreementRepository) => this.transferAgreementRepository = transferAgreementRepository;

    [ProducesResponseType(201)]
    [HttpPost("api/transfer-agreements")]
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

        var response = new TransferAgreementDto(
           Id: result.Id,
           StartDate: result.StartDate.ToUnixTimeSeconds(),
           EndDate: result.EndDate.ToUnixTimeSeconds(),
           SenderName: result.SenderName,
           SenderTin: result.SenderTin,
           ReceiverTin: result.ReceiverTin);

        return Created($"api/transfer-agreements/{response.Id}", response);
    }

    [ProducesResponseType(typeof(TransferAgreementsResponse), 200)]
    [ProducesResponseType(204)]
    [HttpGet("api/transfer-agreements")]
    public async Task<ActionResult<TransferAgreementsResponse>> GetTransferAgreements()
    {
        var subject = User.FindSubjectGuidClaim();
        var userTin = User.FindSubjectTinClaim();

        var transferAgreements = await transferAgreementRepository.GetTransferAgreementsList(Guid.Parse(subject), userTin);

        if (!transferAgreements.Any())
        {
            return NoContent();
        }

        var listResponse = transferAgreements.Select(ta => new TransferAgreementDto(
                ta.Id,
                ta.StartDate.ToUnixTimeSeconds(),
                ta.EndDate.ToUnixTimeSeconds(),
                ta.SenderName,
                ta.SenderTin,
                ta.ReceiverTin))
            .ToList();

        return Ok(new TransferAgreementsResponse(listResponse));
    }

}
