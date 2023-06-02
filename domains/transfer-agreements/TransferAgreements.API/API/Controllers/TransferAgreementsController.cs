using System;
using System.Security.Claims;
using System.Threading.Tasks;
using API.ApiModels.Requests;
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

        var result = await transferAgreementRepository.CreateTransferAgreement(transferAgreement);

        return Created($"api/transfer-agreements/{result.Id}", result);
    }
}
