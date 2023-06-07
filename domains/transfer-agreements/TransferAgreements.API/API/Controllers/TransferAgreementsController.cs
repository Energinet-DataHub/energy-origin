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
        var actor = User.FindActorClaim();
        var subject = User.FindSubjectClaim();

        var transferAgreement = new TransferAgreement
        {
            StartDate = DateTimeOffset.FromUnixTimeSeconds(request.StartDate),
            EndDate = DateTimeOffset.FromUnixTimeSeconds(request.EndDate),
            ActorId = actor,
            SenderId = Guid.Parse(subject),
            ReceiverTin = request.ReceiverTin
        };

        var result = await transferAgreementRepository.AddTransferAgreementToDb(transferAgreement);

        return Created($"api/transfer-agreements/{result.Id}", result);
    }

    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [HttpGet("api/transfer-agreements")]
    public async Task<ActionResult<TransferAgreementsResponse>> GetTransferAgreements()
    {
        var subject = User.FindSubjectClaim();


        var transferAgreements = await transferAgreementRepository.GetTransferAgreementsBySubjectId(Guid.Parse(subject));

        var listResponse = transferAgreements.Select(ta => new TransferAgreementDto(
            ta.Id,
            ta.StartDate.ToUnixTimeSeconds(),
            ta.EndDate.ToUnixTimeSeconds(),
            ta.ReceiverTin)).ToList();

        return Ok(new TransferAgreementsResponse(listResponse));
    }

}
