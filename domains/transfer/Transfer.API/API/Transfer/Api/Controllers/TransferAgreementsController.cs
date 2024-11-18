using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api._Features_;
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
using MediatR;
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
    IMediator mediator,
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
    /// <param name="cancellationToken"></param>
    /// <response code="201">Successful operation</response>
    /// <response code="400">Only the receiver company can accept this Transfer Agreement Proposal or the proposal has run out</response>
    /// <response code="409">There is already a Transfer Agreement with proposals company tin within the selected date range</response>
    [HttpPost()]
    [ProducesResponseType(typeof(TransferAgreement), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(void), 409)]
    public async Task<ActionResult> Create(CreateTransferAgreement request, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var organizationTin = IsOwnOrganization(organizationId) ? identityDescriptor.OrganizationCvr : null;
        var organizationName = IsOwnOrganization(organizationId) ? identityDescriptor.OrganizationName : null;
        var command = new AcceptTransferAgreementProposalCommand(request.TransferAgreementProposalId, organizationId, organizationTin,
            organizationName);
        var result = await mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = result.TransferAgreementId },
            ToTransferAgreementDto(result.TransferAgreementId, result.SenderTin, result.SenderName, result.ReceiverTin, result.StartDate,
                result.EndDate, result.Type));
    }

    private bool IsOwnOrganization(Guid organizationId)
    {
        return identityDescriptor.OrganizationId == organizationId;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TransferAgreementDto), 200)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> Get([FromRoute] Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var result = await unitOfWork.TransferAgreementRepo.GetTransferAgreement(id, organizationId.ToString(), identityDescriptor.OrganizationCvr!,
            cancellationToken);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(TransferAgreementDtoMapper.MapTransferAgreement(result));
    }

    [HttpGet]
    [ProducesResponseType(typeof(TransferAgreementsResponse), 200)]
    public async Task<ActionResult<TransferAgreementsResponse>> GetTransferAgreements([FromQuery] Guid organizationId,
        CancellationToken cancellationToken)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var transferAgreements =
            await unitOfWork.TransferAgreementRepo.GetTransferAgreementsList(organizationId, identityDescriptor.OrganizationCvr!, cancellationToken);

        if (!transferAgreements.Any())
        {
            return Ok(new TransferAgreementsResponse(new List<TransferAgreementDto>()));
        }

        var listResponse = transferAgreements.Select(TransferAgreementDtoMapper.MapTransferAgreement)
            .ToList();

        return Ok(new TransferAgreementsResponse(listResponse));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(void), 404)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 409)]
    public async Task<ActionResult<EditTransferAgreementEndDate>> EditEndDate([FromRoute] Guid id, [FromBody] EditTransferAgreementEndDate request,
        [FromQuery] Guid organizationId, CancellationToken cancellationToken)
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
        var transferAgreement =
            await taRepo.GetTransferAgreement(id, organizationId.ToString(), identityDescriptor.OrganizationCvr!, cancellationToken);

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

        if (await taRepo.HasDateOverlap(overlapQuery, cancellationToken))
        {
            return ValidationProblem("Transfer agreement date overlap", statusCode: 409);
        }

        transferAgreement.EndDate = endDate;

        var response = TransferAgreementDtoMapper.MapTransferAgreement(transferAgreement);

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

    private static TransferAgreementDto ToTransferAgreementDto(Guid transferAgreementId, string senderTin, string senderName, string receiverTin,
        long startDate, long? endDate, TransferAgreementType type)
    {
        return new(
            Id: transferAgreementId,
            StartDate: startDate,
            EndDate: endDate,
            SenderName: senderName,
            SenderTin: senderTin,
            ReceiverTin: receiverTin,
            Type: TransferAgreementTypeMapper.MapCreateTransferAgreementType(type)
        );
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(TransferAgreementProposalOverviewResponse), 200)]
    public async Task<ActionResult<TransferAgreementProposalOverviewResponse>> GetTransferAgreementProposal([FromQuery] Guid organizationId,
        CancellationToken cancellationToken)
    {
        accessDescriptor.AssertAuthorizedToAccessOrganization(organizationId);

        var transferAgreements =
            await unitOfWork.TransferAgreementRepo.GetTransferAgreementsList(organizationId, identityDescriptor.OrganizationCvr!, cancellationToken);
        var transferAgreementProposals = await unitOfWork.TransferAgreementRepo.GetTransferAgreementProposals(organizationId, cancellationToken);

        if (!transferAgreementProposals.Any() && !transferAgreements.Any())
        {
            return Ok(new TransferAgreementProposalOverviewResponse(new()));
        }

        var transferAgreementDtos = transferAgreements
            .Select(x => new TransferAgreementProposalOverviewDto(x.Id, x.StartDate.EpochSeconds, x.EndDate?.EpochSeconds, x.SenderName.Value,
                x.SenderTin.Value, x.ReceiverTin.Value, TransferAgreementTypeMapper.MapCreateTransferAgreementType(x.Type),
                GetTransferAgreementStatusFromAgreement(x)))
            .ToList();

        var transferAgreementProposalDtos = transferAgreementProposals
            .Select(x => new TransferAgreementProposalOverviewDto(x.Id, x.StartDate.EpochSeconds, x.EndDate?.EpochSeconds, string.Empty,
                string.Empty, x.ReceiverCompanyTin?.Value, TransferAgreementTypeMapper.MapCreateTransferAgreementType(x.Type),
                GetTransferAgreementStatusFromProposal(x)))
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
        else if (transferAgreement.StartDate < UnixTimestamp.Now() && transferAgreement.EndDate != null &&
                 transferAgreement.EndDate > UnixTimestamp.Now())
        {
            return TransferAgreementStatus.Active;
        }

        return TransferAgreementStatus.Inactive;
    }

    [HttpPost("create/")]
    [ProducesResponseType(typeof(TransferAgreementDto), 200)]
    [ProducesResponseType(typeof(void), 404)]
    public async Task<ActionResult> CreateTransferAgreementDirectly([FromServices]IProjectOriginWalletClient walletClient, [FromBody] CreateTransferAgreementRequest request)
    {
        accessDescriptor.IsAuthorizedToOrganizations([request.SenderOrganizationId, request.ReceiverOrganizationId]);

        var taRepo = unitOfWork.TransferAgreementRepo;

        var transferAgreement = new TransferAgreement
        {
            StartDate = UnixTimestamp.Create(request.StartDate),
            EndDate = request.EndDate.HasValue ? UnixTimestamp.Create(request.EndDate.Value) : null,
            SenderId = OrganizationId.Create(request.SenderOrganizationId),
            SenderName = OrganizationName.Create(request.SenderName),
            SenderTin = Tin.Create(request.SenderTin),
            ReceiverName = OrganizationName.Create(request.ReceiverName),
            ReceiverTin = Tin.Create(request.ReceiverTin)
        };

        var hasConflict = await taRepo.HasDateOverlap(transferAgreement, CancellationToken.None);
        if (hasConflict)
        {
            return ValidationProblem("There is already a Transfer Agreement with proposals company tin within the selected date range",
                statusCode: 409);
        }

        var wallets = await walletClient.GetWallets(request.ReceiverOrganizationId, CancellationToken.None);

        var walletId = wallets.Result.FirstOrDefault()?.Id;
        if (walletId == null) // TODO: This code should be deleted when we allign when and where we create a wallet. 🐉
        {
            var createWalletResponse = await walletClient.CreateWallet(request.ReceiverOrganizationId, CancellationToken.None);

            if (createWalletResponse == null)
                throw new ApplicationException("Failed to create wallet.");

            walletId = createWalletResponse.WalletId;
        }

        var walletEndpoint = await walletClient.CreateWalletEndpoint(request.ReceiverOrganizationId, walletId.Value, CancellationToken.None);

        var externalEndpoint = await walletClient.CreateExternalEndpoint(request.SenderOrganizationId, walletEndpoint, request.SenderTin, CancellationToken.None);

        transferAgreement.ReceiverReference = externalEndpoint.ReceiverId;

        try
        {
            var result = await taRepo.AddTransferAgreement(transferAgreement, CancellationToken.None);

            await unitOfWork.SaveAsync();

            return CreatedAtAction(nameof(Get), new { id = result.Id }, ToTransferAgreementDto(result.Id, result.SenderTin.Value, result.SenderName.Value,
                result.ReceiverTin.Value, result.StartDate.EpochSeconds, result.EndDate?.EpochSeconds, result.Type));
        }
        catch (DbUpdateException)
        {
            return ValidationProblem(statusCode: 409);
        }

    }
}


