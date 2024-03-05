using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using API.Shared.Helpers;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using API.Transfer.Api.Repository;
using API.Transfer.Api.Services;
using Asp.Versioning;
using DataContext.Models;
using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Transfer.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.Version20240103)]
[Route("api/transfer/transfer-agreements")]
public class TransferAgreementsController(
    ITransferAgreementRepository transferAgreementRepository,
    IProjectOriginWalletService projectOriginWalletService,
    IHttpContextAccessor httpContextAccessor,
    ITransferAgreementProposalRepository transferAgreementProposalRepository,
    IActivityLogEntryRepository activityLogEntryRepository
    ) : ControllerBase
{
    /// <summary>
    /// Add a new Transfer Agreement
    /// </summary>
    /// <param name="request">The request object containing the TransferAgreementProposalId for creating the Transfer Agreement.</param>
    /// <response code="201">Successful operation</response>
    /// <response code="400">Only the receiver company can accept this Transfer Agreement Proposal or the proposal has run out</response>
    /// <response code="409">There is already a Transfer Agreement with proposals company tin within the selected date range</response>
    [Authorize(Policy = PolicyName.RequiresCompany)]
    [HttpPost]
    [ProducesResponseType(typeof(TransferAgreement), 201)]
    [ProducesResponseType(typeof(void), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 409)]
    public async Task<ActionResult> Create(CreateTransferAgreement request)
    {
        if (request.TransferAgreementProposalId == Guid.Empty)
        {
            return ValidationProblem("Must set TransferAgreementProposalId");
        }

        var proposal =
            await transferAgreementProposalRepository.GetNonExpiredTransferAgreementProposalAsNoTracking(request.TransferAgreementProposalId);
        if (proposal == null)
        {
            return NotFound();
        }

        var user = new UserDescriptor(HttpContext.User);

        if (proposal.ReceiverCompanyTin != null && proposal.ReceiverCompanyTin != user.Organization!.Tin)
        {
            return ValidationProblem("Only the receiver company can accept this Transfer Agreement Proposal");
        }

        if (proposal.EndDate < DateTimeOffset.UtcNow)
        {
            return ValidationProblem("This proposal has run out");
        }

        proposal.ReceiverCompanyTin ??= user.Organization!.Tin;

        var hasConflict = await transferAgreementRepository.HasDateOverlap(proposal);
        if (hasConflict)
        {
            return ValidationProblem("There is already a Transfer Agreement with proposals company tin within the selected date range",
                statusCode: 409);
        }

        var receiverBearerToken = AuthenticationHeaderValue.Parse(httpContextAccessor.HttpContext?.Request.Headers["Authorization"]!);
        var receiverWdeBase64String = await projectOriginWalletService.CreateWalletDepositEndpoint(receiverBearerToken);

        var senderBearerToken = ProjectOriginWalletHelper.GenerateBearerToken(proposal.SenderCompanyId.ToString());

        var receiverReference = await projectOriginWalletService.CreateReceiverDepositEndpoint(
            new AuthenticationHeaderValue("Bearer", senderBearerToken),
            receiverWdeBase64String,
            proposal.ReceiverCompanyTin);

        var transferAgreement = new TransferAgreement
        {
            StartDate = proposal.StartDate,
            EndDate = proposal.EndDate,
            SenderId = proposal.SenderCompanyId,
            SenderName = proposal.SenderCompanyName,
            SenderTin = proposal.SenderCompanyTin,
            ReceiverTin = proposal.ReceiverCompanyTin,
            ReceiverReference = receiverReference
        };

        try
        {
            var result = await transferAgreementRepository.AddTransferAgreementAndDeleteProposal(transferAgreement,
                request.TransferAgreementProposalId);

            await AppendProposalAcceptedToActivityLog(user, result);

            return CreatedAtAction(nameof(Get), new { id = result.Id }, ToTransferAgreementDto(result));
        }
        catch (DbUpdateException)
        {
            return ValidationProblem(statusCode: 409);
        }
    }

    private async Task AppendProposalAcceptedToActivityLog(UserDescriptor user, TransferAgreement result)
    {
        // Receiver entry
        await activityLogEntryRepository.AddActivityLogEntryAsync(ActivityLogEntry.Create(user.Subject, ActivityLogEntry.ActorTypeEnum.User,
            user.Name, user.Organization!.Tin, user.Organization.Name, string.Empty, string.Empty, ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            ActivityLogEntry.ActionTypeEnum.Accepted, result.Id.ToString()));

        // Sender entry
        await activityLogEntryRepository.AddActivityLogEntryAsync(ActivityLogEntry.Create(user.Subject, ActivityLogEntry.ActorTypeEnum.User,
            string.Empty, result.SenderTin, result.SenderName, string.Empty, string.Empty, ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            ActivityLogEntry.ActionTypeEnum.Accepted, result.Id.ToString()));
    }

    [Authorize(Policy = PolicyName.RequiresCompany)]
    [ProducesResponseType(typeof(TransferAgreementDto), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{id}")]
    public async Task<ActionResult> Get([FromRoute] Guid id)
    {
        var user = new UserDescriptor(User);

        var result = await transferAgreementRepository.GetTransferAgreement(id, user.Subject.ToString(), user.Organization!.Tin);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(ToTransferAgreementDto(result));
    }

    [Authorize(Policy = PolicyName.RequiresCompany)]
    [ProducesResponseType(typeof(TransferAgreementsResponse), 200)]
    [HttpGet]
    public async Task<ActionResult<TransferAgreementsResponse>> GetTransferAgreements()
    {
        var user = new UserDescriptor(User);

        var transferAgreements = await transferAgreementRepository.GetTransferAgreementsList(user.Subject, user.Organization!.Tin);

        if (!transferAgreements.Any())
        {
            return Ok(new TransferAgreementsResponse(new List<TransferAgreementDto>()));
        }

        var listResponse = transferAgreements.Select(ToTransferAgreementDto)
            .ToList();

        return Ok(new TransferAgreementsResponse(listResponse));
    }

    [Authorize(Policy = PolicyName.RequiresCompany)]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(409)]
    [HttpPut("{id}")]
    public async Task<ActionResult<EditTransferAgreementEndDate>> EditEndDate(Guid id, [FromBody] EditTransferAgreementEndDate request)
    {
        var user = new UserDescriptor(User);

        var validator = new EditTransferAgreementEndDateValidator();

        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var endDate = request.EndDate.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(request.EndDate.Value)
            : (DateTimeOffset?)null;

        var transferAgreement = await transferAgreementRepository.GetTransferAgreement(id, user.Subject.ToString(), user.Organization!.Tin);

        if (transferAgreement == null || transferAgreement.SenderId != user.Subject)
        {
            return NotFound();
        }

        if (transferAgreement.EndDate < DateTimeOffset.UtcNow)
            return ValidationProblem("Transfer agreement has expired");

        var overlapQuery = new TransferAgreement
        {
            Id = transferAgreement.Id,
            StartDate = transferAgreement.StartDate,
            EndDate = endDate,
            SenderId = transferAgreement.SenderId,
            ReceiverTin = transferAgreement.ReceiverTin
        };

        if (await transferAgreementRepository.HasDateOverlap(overlapQuery))
        {
            return ValidationProblem("Transfer agreement date overlap", statusCode: 409);
        }

        transferAgreement.EndDate = endDate;

        await transferAgreementRepository.Save();

        var response = new TransferAgreementDto(
            Id: transferAgreement.Id,
            StartDate: transferAgreement.StartDate.ToUnixTimeSeconds(),
            EndDate: transferAgreement.EndDate?.ToUnixTimeSeconds(),
            SenderName: transferAgreement.SenderName,
            SenderTin: transferAgreement.SenderTin,
            ReceiverTin: transferAgreement.ReceiverTin);

        await AppendAgreementEndDateChangedToActivityLog(user, transferAgreement);

        return Ok(response);
    }

    private async Task AppendAgreementEndDateChangedToActivityLog(UserDescriptor user, TransferAgreement result)
    {
        // Receiver entry
        await activityLogEntryRepository.AddActivityLogEntryAsync(ActivityLogEntry.Create(user.Subject, ActivityLogEntry.ActorTypeEnum.User,
            String.Empty, result.ReceiverTin, String.Empty, string.Empty, string.Empty, ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            ActivityLogEntry.ActionTypeEnum.EndDateChanged, result.Id.ToString()));

        // Sender entry
        await activityLogEntryRepository.AddActivityLogEntryAsync(ActivityLogEntry.Create(user.Subject, ActivityLogEntry.ActorTypeEnum.User,
            user.Name, result.SenderTin, result.SenderName, string.Empty, string.Empty, ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            ActivityLogEntry.ActionTypeEnum.EndDateChanged, result.Id.ToString()));
    }

    private static TransferAgreementDto ToTransferAgreementDto(TransferAgreement transferAgreement) =>
        new(
            Id: transferAgreement.Id,
            StartDate: transferAgreement.StartDate.ToUnixTimeSeconds(),
            EndDate: transferAgreement.EndDate?.ToUnixTimeSeconds(),
            SenderName: transferAgreement.SenderName,
            SenderTin: transferAgreement.SenderTin,
            ReceiverTin: transferAgreement.ReceiverTin
        );
}
