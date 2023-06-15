using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.ApiModels.Requests;
using API.ApiModels.Responses;
using API.Data;
using API.Extensions;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [HttpPatch("api/transfer-agreements/{id}")]
    public async Task<ActionResult<EditTransferAgreementEndDate>> EditEndDate(Guid id, [FromBody] EditTransferAgreementEndDate request)
    {
        var validator = new EditTransferAgreementEndDateValidator();

        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var endDate = DateTimeOffset.FromUnixTimeSeconds(request.EndDate);
        var senderId = Guid.Parse(User.FindSubjectGuidClaim());
        var transferAgreement = await transferAgreementRepository.GetTransferAgreement(id);

        if (transferAgreement == null || transferAgreement.SenderId != senderId)
        {
            return NotFound();
        }

        if (transferAgreement.EndDate < DateTimeOffset.UtcNow)
            return ValidationProblem("Transfer agreement has expired");

        if (await transferAgreementRepository.HasDateOverlap(id, endDate, senderId, transferAgreement.ReceiverTin))
            return Conflict("Transfer agreement date overlap");

        transferAgreement.EndDate = endDate;

        await transferAgreementRepository.Save();

            var response = new TransferAgreementDto(
                Id: transferAgreement.Id,
                StartDate: transferAgreement.StartDate.ToUnixTimeSeconds(),
                EndDate: transferAgreement.EndDate.ToUnixTimeSeconds(),
                SenderName: transferAgreement.SenderName,
                SenderTin: transferAgreement.SenderTin,
                ReceiverTin: transferAgreement.ReceiverTin);

            return Ok(response);
        }

    }
