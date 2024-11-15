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
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup;
using EnergyOrigin.TokenValidation.b2c;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectOriginClients;

namespace API.Transfer.Api.Controllers;

[Authorize(Policy.Frontend)]
[ApiController]
[ApiVersion(ApiVersions.Version1)]
[ApiVersion(ApiVersions.Version20240515, Deprecated = true)]
[Route("api/transfer/transfer-agreements")]
public class TransferAgreementsController(
    IProjectOriginWalletClient walletClient,
    IUnitOfWork unitOfWork,
    IdentityDescriptor identityDescriptor,
    AccessDescriptor accessDescriptor
) : ControllerBase
{
    /// <summary>
    /// Add a new Transfer Agreement
    /// </summary>
    /// <param name="request">The request object containing the TransferAgreementProposalId for creating the Transfer Agreement.</param>
    /// <param name="organizationId"></param>
    /// <response code="201">Successful operation</response>
    /// <response code="400">Only the receiver company can accept this Transfer Agreement Proposal or the proposal has run out</response>
    /// <response code="409">There is already a Transfer Agreement with proposals company tin within the selected date range</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransferAgreement), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 409)]
    public async Task<ActionResult> Create(CreateTransferAgreement request, [FromQuery] Guid organizationId)
    {
        if (request.TransferAgreementProposalId == Guid.Empty)
        {
            return ValidationProblem("Must set TransferAgreementProposalId");
        }

        var proposal =
            await unitOfWork.TransferAgreementProposalRepo.GetNonExpiredTransferAgreementProposalAsNoTracking(request.TransferAgreementProposalId);
        if (proposal == null)
        {
            return NotFound();
        }

        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        if (proposal.ReceiverCompanyTin != null && proposal.ReceiverCompanyTin.Value != identityDescriptor.OrganizationCvr)
        {
            return ValidationProblem("Only the receiver company can accept this Transfer Agreement Proposal");
        }

        if (proposal.EndDate != null && proposal.EndDate < UnixTimestamp.Now())
        {
            return ValidationProblem("This proposal has run out");
        }

        proposal.ReceiverCompanyTin = Tin.Create(identityDescriptor.OrganizationCvr!);

        var taRepo = unitOfWork.TransferAgreementRepo;

        var hasConflict = await taRepo.HasDateOverlap(proposal);
        if (hasConflict)
        {
            return ValidationProblem("There is already a Transfer Agreement with proposals company tin within the selected date range",
                statusCode: 409);
        }

        var subject = identityDescriptor.Subject;
        var wallets = await walletClient.GetWallets(subject, CancellationToken.None);

        var walletId = wallets.Result.FirstOrDefault()?.Id;
        if (walletId == null)
        {
            var createWalletResponse = await walletClient.CreateWallet(identityDescriptor.Subject, CancellationToken.None);

            if (createWalletResponse == null)
                throw new ApplicationException("Failed to create wallet.");

            walletId = createWalletResponse.WalletId;
        }

        var walletEndpoint = await walletClient.CreateWalletEndpoint(subject, walletId.Value, CancellationToken.None);

        var externalEndpoint =
            await walletClient.CreateExternalEndpoint(proposal.SenderCompanyId.Value, walletEndpoint, proposal.ReceiverCompanyTin.Value, CancellationToken.None);

        var transferAgreement = new TransferAgreement
        {
            StartDate = proposal.StartDate,
            EndDate = proposal.EndDate,
            SenderId = proposal.SenderCompanyId,
            SenderName = proposal.SenderCompanyName,
            SenderTin = proposal.SenderCompanyTin,
            ReceiverName = OrganizationName.Create(identityDescriptor.OrganizationName),
            ReceiverTin = proposal.ReceiverCompanyTin,
            ReceiverReference = externalEndpoint.ReceiverId
        };

        try
        {
            var result = await taRepo.AddTransferAgreementAndDeleteProposal(transferAgreement,
                request.TransferAgreementProposalId);

            await AppendProposalAcceptedToActivityLog(identityDescriptor, result, proposal);

            await unitOfWork.SaveAsync();

            return CreatedAtAction(nameof(Get), new { id = result.Id }, ToTransferAgreementDto(result));
        }
        catch (DbUpdateException)
        {
            return ValidationProblem(statusCode: 409);
        }
    }

    private async Task AppendProposalAcceptedToActivityLog(IdentityDescriptor identity, TransferAgreement result, TransferAgreementProposal proposal)
    {
        // Receiver entry
        await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
            actorId: identity.Subject,
            actorType: ActivityLogEntry.ActorTypeEnum.User,
            actorName: identity.Name,
            organizationTin: identity.OrganizationCvr!,
            organizationName: identity.OrganizationName,
            otherOrganizationTin: proposal.SenderCompanyTin.Value,
            otherOrganizationName: proposal.SenderCompanyName.Value,
            entityType: ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            actionType: ActivityLogEntry.ActionTypeEnum.Accepted,
            entityId: result.Id.ToString())
        );

        // Sender entry
        await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
            actorId: Guid.Empty,
            actorType: ActivityLogEntry.ActorTypeEnum.User,
            actorName: string.Empty,
            organizationTin: proposal.SenderCompanyTin.Value,
            organizationName: proposal.SenderCompanyName.Value,
            otherOrganizationTin: result.ReceiverTin.Value,
            otherOrganizationName: result.ReceiverName.Value,
            entityType: ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            actionType: ActivityLogEntry.ActionTypeEnum.Accepted,
            entityId: result.Id.ToString())
        );
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransferAgreementDto), 200)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> Get([FromRoute] Guid id, [FromQuery] Guid organizationId)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var result = await unitOfWork.TransferAgreementRepo.GetTransferAgreement(id, organizationId.ToString(), identityDescriptor.OrganizationCvr!);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(ToTransferAgreementDto(result));
    }

    [HttpGet]
    [ProducesResponseType(typeof(TransferAgreementsResponse), 200)]
    public async Task<ActionResult<TransferAgreementsResponse>> GetTransferAgreements([FromQuery] Guid organizationId)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var transferAgreements = await unitOfWork.TransferAgreementRepo.GetTransferAgreementsList(organizationId, identityDescriptor.OrganizationCvr!);

        if (!transferAgreements.Any())
        {
            return Ok(new TransferAgreementsResponse(new List<TransferAgreementDto>()));
        }

        var listResponse = transferAgreements.Select(ToTransferAgreementDto)
            .ToList();

        return Ok(new TransferAgreementsResponse(listResponse));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 409)]
    public async Task<ActionResult<EditTransferAgreementEndDate>> EditEndDate([FromRoute] Guid id, [FromBody] EditTransferAgreementEndDate request, [FromQuery] Guid organizationId)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var validator = new EditTransferAgreementEndDateValidator();

        var validateResult = await validator.ValidateAsync(request);
        if (!validateResult.IsValid)
        {
            validateResult.AddToModelState(ModelState);
            return ValidationProblem(ModelState);
        }

        var endDate = request.EndDate.HasValue
            ? UnixTimestamp.Create(request.EndDate.Value)
            : null;

        var taRepo = unitOfWork.TransferAgreementRepo;
        var transferAgreement = await taRepo.GetTransferAgreement(id, organizationId.ToString(), identityDescriptor.OrganizationCvr!);

        if (transferAgreement == null || transferAgreement.SenderId.Value != organizationId)
        {
            return NotFound();
        }

        if (transferAgreement.EndDate != null && transferAgreement.EndDate < UnixTimestamp.Now())
        {
            return ValidationProblem("Transfer agreement has expired");
        }

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
            StartDate: transferAgreement.StartDate.EpochSeconds,
            EndDate: transferAgreement.EndDate?.EpochSeconds,
            SenderName: transferAgreement.SenderName.Value,
            SenderTin: transferAgreement.SenderTin.Value,
            ReceiverTin: transferAgreement.ReceiverTin.Value);

        await AppendAgreementEndDateChangedToActivityLog(identityDescriptor, transferAgreement);

        await unitOfWork.SaveAsync();

        return Ok(response);
    }

    private async Task AppendAgreementEndDateChangedToActivityLog(IdentityDescriptor identity, TransferAgreement result)
    {
        // Receiver entry
        await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
            actorId: identity.Subject,
            actorType: ActivityLogEntry.ActorTypeEnum.User,
            actorName: String.Empty,
            organizationTin: result.ReceiverTin.Value,
            organizationName: result.ReceiverName.Value,
            otherOrganizationTin: identity.OrganizationCvr!,
            otherOrganizationName: identity.OrganizationName,
            entityType: ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            actionType: ActivityLogEntry.ActionTypeEnum.EndDateChanged,
            entityId: result.Id.ToString())
        );

        // Sender entry
        await unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
            actorId: identity.Subject,
            actorType: ActivityLogEntry.ActorTypeEnum.User,
            actorName: identity.Name,
            organizationTin: identity.OrganizationCvr!,
            organizationName: identity.OrganizationName,
            otherOrganizationTin: result.ReceiverTin.Value,
            otherOrganizationName: result.ReceiverName.Value,
            entityType: ActivityLogEntry.EntityTypeEnum.TransferAgreement,
            actionType: ActivityLogEntry.ActionTypeEnum.EndDateChanged,
            entityId: result.Id.ToString())
        );
    }

    private static TransferAgreementDto ToTransferAgreementDto(TransferAgreement transferAgreement) =>
        new(
            Id: transferAgreement.Id,
            StartDate: transferAgreement.StartDate.EpochSeconds,
            EndDate: transferAgreement.EndDate?.EpochSeconds,
            SenderName: transferAgreement.SenderName.Value,
            SenderTin: transferAgreement.SenderTin.Value,
            ReceiverTin: transferAgreement.ReceiverTin.Value
        );

    [HttpGet("overview")]
    [ProducesResponseType(typeof(TransferAgreementProposalOverviewResponse), 200)]
    public async Task<ActionResult<TransferAgreementProposalOverviewResponse>> GetTransferAgreementProposal([FromQuery] Guid organizationId)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var transferAgreements = await unitOfWork.TransferAgreementRepo.GetTransferAgreementsList(organizationId, identityDescriptor.OrganizationCvr!);
        var transferAgreementProposals = await unitOfWork.TransferAgreementRepo.GetTransferAgreementProposals(organizationId);

        if (!transferAgreementProposals.Any() && !transferAgreements.Any())
        {
            return Ok(new TransferAgreementProposalOverviewResponse(new()));
        }

        var transferAgreementDtos = transferAgreements
            .Select(x => new TransferAgreementProposalOverviewDto(x.Id, x.StartDate.EpochSeconds, x.EndDate?.EpochSeconds, x.SenderName.Value,
                x.SenderTin.Value, x.ReceiverTin.Value, GetTransferAgreementStatusFromAgreement(x)))
            .ToList();

        var transferAgreementProposalDtos = transferAgreementProposals
            .Select(x => new TransferAgreementProposalOverviewDto(x.Id, x.StartDate.EpochSeconds, x.EndDate?.EpochSeconds, string.Empty,
                string.Empty, x.ReceiverCompanyTin?.Value, GetTransferAgreementStatusFromProposal(x)))
            .ToList();

        transferAgreementProposalDtos.AddRange(transferAgreementDtos);

        return Ok(new TransferAgreementProposalOverviewResponse(transferAgreementProposalDtos));
    }

    private TransferAgreementStatus GetTransferAgreementStatusFromProposal(TransferAgreementProposal transferAgreementProposal)
    {
        var timespan = DateTimeOffset.UtcNow - transferAgreementProposal.CreatedAt.ToDateTimeOffset();

        return timespan.Days switch
        {
            >= 0 and <= 14 => TransferAgreementStatus.Proposal,
            _ => TransferAgreementStatus.ProposalExpired
        };
    }

    private TransferAgreementStatus GetTransferAgreementStatusFromAgreement(TransferAgreement transferAgreement)
    {
        if (transferAgreement.StartDate <= UnixTimestamp.Now() && transferAgreement.EndDate == null)
        {
            return TransferAgreementStatus.Active;
        }
        else if (transferAgreement.StartDate < UnixTimestamp.Now() && transferAgreement.EndDate != null && transferAgreement.EndDate > UnixTimestamp.Now())
        {
            return TransferAgreementStatus.Active;
        }

        return TransferAgreementStatus.Inactive;
    }

    [HttpPost("create/")]
    [ProducesResponseType(typeof(TransferAgreementDto), 200)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> CreateTransferAgreementDirectly([FromBody] CreateTransferAgreementRequest request,[FromQuery] Guid organizationId)
    {
        accessDescriptor.IsAuthorizedToOrganizations([request.transferGiverOrganizationId, request.transferReceiverOrganizationId]);

        var taRepo = unitOfWork.TransferAgreementRepo;

        // We should check if there is conflicts before going on.
        // var hasConflict = await taRepo.HasDateOverlap(proposal);
        // if (hasConflict)
        // {
        //     return ValidationProblem("There is already a Transfer Agreement with proposals company tin within the selected date range",
        //         statusCode: 409);
        // }

        var subject = identityDescriptor.Subject;
        var wallets = await walletClient.GetWallets(subject, CancellationToken.None);

        var walletId = wallets.Result.FirstOrDefault()?.Id;
        if (walletId == null) // TODO: This code should be deleted when we allign when and where we create a wallet. 🐉
        {
            var createWalletResponse = await walletClient.CreateWallet(identityDescriptor.Subject, CancellationToken.None);

            if (createWalletResponse == null)
                throw new ApplicationException("Failed to create wallet.");

            walletId = createWalletResponse.WalletId;
        }

        var walletEndpoint = await walletClient.CreateWalletEndpoint(subject, walletId.Value, CancellationToken.None);

        var externalEndpoint =
            await walletClient.CreateExternalEndpoint(request.transferGiverOrganizationId, walletEndpoint, request.transferReceiverOrganizationId.ToString(), CancellationToken.None);

        var transferAgreement = new TransferAgreement
        {
            StartDate = UnixTimestamp.Create(request.startDate),
            EndDate = request.endDate.HasValue ? UnixTimestamp.Create(request.endDate.Value) : null,
            SenderId = OrganizationId.Create(request.transferGiverOrganizationId),
            SenderName = OrganizationName.Empty(), // TODO: We can set this if it's the logged in user. But I would rather fix this as part of bug fix.
            SenderTin = Tin.Empty(), // TODO: We can set this if it's the logged in user. But I would rather fix this as part of bug fix.
            ReceiverName = OrganizationName.Empty(),
            ReceiverTin = Tin.Empty(), // TODO: We can set this if it's the logged in user. But I would rather fix this as part of bug fix.
            ReceiverReference = externalEndpoint.ReceiverId
        };

        try
        {
            var result = await taRepo.AddTransferAgreement(transferAgreement);

            await unitOfWork.SaveAsync();

            return CreatedAtAction(nameof(Get), new { id = result.Id }, ToTransferAgreementDto(result));
        }
        catch (DbUpdateException)
        {
            return ValidationProblem(statusCode: 409);
        }

    }
}

public record CreateTransferAgreementRequest(Guid transferReceiverOrganizationId, Guid transferGiverOrganizationId, long startDate, long? endDate);
