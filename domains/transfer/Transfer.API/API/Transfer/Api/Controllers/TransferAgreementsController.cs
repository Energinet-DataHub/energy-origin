using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Dto.Requests;
using API.Transfer.Api.Dto.Responses;
using API.UnitOfWork;
using Asp.Versioning;
using DataContext.Models;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Swagger;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOriginClients;

namespace API.Transfer.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.Version20240103)]
[Route("api/transfer/transfer-agreements")]
public class TransferAgreementsController(
    IProjectOriginWalletClient walletClient,
    IUnitOfWork unitOfWork
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
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 409)]
    public async Task<ActionResult> Create(CreateTransferAgreement request)
    {
        if (request.TransferAgreementProposalId == Guid.Empty)
        {
            return ValidationProblem("Must set TransferAgreementProposalId");
        }

        var proposal = await unitOfWork.TransferAgreementProposalRepo.GetNonExpiredTransferAgreementProposalAsNoTracking(request.TransferAgreementProposalId);
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

        var taRepo = unitOfWork.TransferAgreementRepo;

        var hasConflict = await taRepo.HasDateOverlap(proposal);
        if (hasConflict)
        {
            return ValidationProblem("There is already a Transfer Agreement with proposals company tin within the selected date range",
                statusCode: 409);
        }

        var subject = user.Subject;
        var wallets = await walletClient.GetWallets(subject, CancellationToken.None);

        var walletId = wallets.Result.FirstOrDefault()?.Id;
        if (walletId == null)
        {
            var createWalletResponse = await walletClient.CreateWallet(user.Subject, CancellationToken.None);

            if (createWalletResponse == null)
                throw new ApplicationException("Failed to create wallet.");

            walletId = createWalletResponse.WalletId;
        }

        var walletEndpoint = await walletClient.CreateWalletEndpoint(subject, walletId.Value, CancellationToken.None);

        var externalEndpoint = await walletClient.CreateExternalEndpoint(proposal.SenderCompanyId, walletEndpoint, proposal.ReceiverCompanyTin, CancellationToken.None);

        var transferAgreement = new TransferAgreement
        {
            StartDate = proposal.StartDate,
            EndDate = proposal.EndDate,
            SenderId = proposal.SenderCompanyId,
            SenderName = proposal.SenderCompanyName,
            SenderTin = proposal.SenderCompanyTin,
            ReceiverName = user.Organization!.Name,
            ReceiverTin = proposal.ReceiverCompanyTin,
            ReceiverReference = externalEndpoint.ReceiverId
        };

        try
        {
            var result = await taRepo.AddTransferAgreementAndDeleteProposal(transferAgreement,
                request.TransferAgreementProposalId);

            await AppendProposalAcceptedToActivityLog(user, result, proposal);

            await unitOfWork.SaveAsync();

            return CreatedAtAction(nameof(Get), new { id = result.Id }, ToTransferAgreementDto(result));
        }
        catch (DbUpdateException)
        {
            return ValidationProblem(statusCode: 409);
        }
    }

    private async Task AppendProposalAcceptedToActivityLog(UserDescriptor user, TransferAgreement result, TransferAgreementProposal proposal)
    {
        // Receiver entry
        await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
            actorId: user.Subject,
            actorType: ActivityLogEntry.ActorTypeEnum.User,
            actorName: user.Name,
            organizationTin: user.Organization!.Tin,
            organizationName: user.Organization.Name,
            otherOrganizationTin: proposal.SenderCompanyTin,
            otherOrganizationName: proposal.SenderCompanyName,
            entityType: ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            actionType: ActivityLogEntry.ActionTypeEnum.Accepted,
            entityId: result.Id.ToString())
        );

        // Sender entry
        await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
            actorId: Guid.Empty,
            actorType: ActivityLogEntry.ActorTypeEnum.User,
            actorName: string.Empty,
            organizationTin: proposal.SenderCompanyTin,
            organizationName: proposal.SenderCompanyName,
            otherOrganizationTin: result.ReceiverTin,
            otherOrganizationName: result.ReceiverName,
            entityType: ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            actionType: ActivityLogEntry.ActionTypeEnum.Accepted,
            entityId: result.Id.ToString())
        );
    }

    [Authorize(Policy = PolicyName.RequiresCompany)]
    [ProducesResponseType(typeof(TransferAgreementDto), 200)]
    [ProducesResponseType(typeof(void), 404)]
    [HttpGet("{id}")]
    public async Task<ActionResult> Get([FromRoute] Guid id)
    {
        var user = new UserDescriptor(User);

        var result = await unitOfWork.TransferAgreementRepo.GetTransferAgreement(id, user.Subject.ToString(), user.Organization!.Tin);

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

        var transferAgreements = await unitOfWork.TransferAgreementRepo.GetTransferAgreementsList(user.Subject, user.Organization!.Tin);

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
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 409)]
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

        var taRepo = unitOfWork.TransferAgreementRepo;
        var transferAgreement = await taRepo.GetTransferAgreement(id, user.Subject.ToString(), user.Organization!.Tin);

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

        if (await taRepo.HasDateOverlap(overlapQuery))
        {
            return ValidationProblem("Transfer agreement date overlap", statusCode: 409);
        }

        transferAgreement.EndDate = endDate;

        var response = new TransferAgreementDto(
            Id: transferAgreement.Id,
            StartDate: transferAgreement.StartDate.ToUnixTimeSeconds(),
            EndDate: transferAgreement.EndDate?.ToUnixTimeSeconds(),
            SenderName: transferAgreement.SenderName,
            SenderTin: transferAgreement.SenderTin,
            ReceiverTin: transferAgreement.ReceiverTin);

        await AppendAgreementEndDateChangedToActivityLog(user, transferAgreement);

        await unitOfWork.SaveAsync();

        return Ok(response);
    }

    private async Task AppendAgreementEndDateChangedToActivityLog(UserDescriptor user, TransferAgreement result)
    {
        // Receiver entry
        await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
            actorId: user.Subject,
            actorType: ActivityLogEntry.ActorTypeEnum.User,
            actorName: String.Empty,
            organizationTin: result.ReceiverTin,
            organizationName: result.ReceiverName,
            otherOrganizationTin: user.Organization!.Tin,
            otherOrganizationName: user.Organization.Name,
            entityType: ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            actionType: ActivityLogEntry.ActionTypeEnum.EndDateChanged,
            entityId: result.Id.ToString())
        );

        // Sender entry
        await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
            actorId: user.Subject,
            actorType: ActivityLogEntry.ActorTypeEnum.User,
            actorName: user.Name,
            organizationTin: user.Organization!.Tin,
            organizationName: user.Organization.Name,
            otherOrganizationTin: result.ReceiverTin,
            otherOrganizationName: result.ReceiverName,
            entityType: ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            actionType: ActivityLogEntry.ActionTypeEnum.EndDateChanged,
            entityId: result.Id.ToString())
        );
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

    [Authorize(Policy = PolicyName.RequiresCompany)]
    [ProducesResponseType(typeof(TransferAgreementProposalOverviewResponse), 200)]
    [HttpGet("overview")]
    public async Task<ActionResult<TransferAgreementProposalOverviewResponse>> GetTransferAgreementProposal()
    {
        var user = new UserDescriptor(User);

        var transferAgreements = await unitOfWork.TransferAgreementRepo.GetTransferAgreementsList(user.Subject, user.Organization!.Tin);
        var transferAgreementProposals = await unitOfWork.TransferAgreementRepo.GetTransferAgreementProposals(user.Subject);

        if (!transferAgreementProposals.Any() && !transferAgreements.Any())
        {
            return Ok(new TransferAgreementProposalOverviewResponse(new()));
        }

        var transferAgreementDtos = transferAgreements
            .Select(x => new TransferAgreementProposalOverviewDto(x.Id, x.StartDate.ToUnixTimeSeconds(), x.EndDate?.ToUnixTimeSeconds(), x.SenderName, x.SenderTin, x.ReceiverTin, GetTransferAgreementStatusFromAgreement(x)))
            .ToList();

        var transferAgreementProposalDtos = transferAgreementProposals
            .Select(x => new TransferAgreementProposalOverviewDto(x.Id, x.StartDate.ToUnixTimeSeconds(), x.EndDate?.ToUnixTimeSeconds(), string.Empty, string.Empty, x.ReceiverCompanyTin, GetTransferAgreementStatusFromProposal(x)))
            .ToList();

        transferAgreementProposalDtos.AddRange(transferAgreementDtos);

        return Ok(new TransferAgreementProposalOverviewResponse(transferAgreementProposalDtos));
    }

    private TransferAgreementStatus GetTransferAgreementStatusFromProposal(TransferAgreementProposal transferAgreementProposal)
    {
        var timespan = DateTimeOffset.UtcNow - transferAgreementProposal.CreatedAt;

        return timespan.Days switch
        {
            >= 0 and <= 14 => TransferAgreementStatus.Proposal,
            _ => TransferAgreementStatus.ProposalExpired
        };
    }

    private TransferAgreementStatus GetTransferAgreementStatusFromAgreement(TransferAgreement transferAgreement)
    {
        if (transferAgreement.StartDate <= DateTimeOffset.UtcNow && transferAgreement.EndDate == null)
        {
            return TransferAgreementStatus.Active;
        }
        else if (transferAgreement.StartDate < DateTimeOffset.UtcNow && transferAgreement.EndDate > DateTimeOffset.UtcNow)
        {
            return TransferAgreementStatus.Active;
        }

        return TransferAgreementStatus.Inactive;
    }
}
