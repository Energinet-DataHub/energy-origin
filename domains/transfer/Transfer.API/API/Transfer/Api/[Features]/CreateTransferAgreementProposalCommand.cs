using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Clients;
using API.Transfer.Api.Exceptions;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using Microsoft.Extensions.Logging;

namespace API.Transfer.Api._Features_;

public class CreateTransferAgreementProposalCommand : IRequest<CreateTransferAgreementProposalCommandResult>
{
    public OrganizationId SenderOrganizationId { get; }
    public Tin ReceiverOrganizationTin { get; }
    public UnixTimestamp StartDate { get; }
    public UnixTimestamp? EndDate { get; }
    public TransferAgreementType Type { get; }

    public CreateTransferAgreementProposalCommand(Guid senderOrganizationId, string? receiverOrganizationTin, long startDate, long? endDate, TransferAgreementType type)
    {
        ReceiverOrganizationTin = receiverOrganizationTin is not null ? Tin.Create(receiverOrganizationTin) : Tin.Empty();
        SenderOrganizationId = OrganizationId.Create(senderOrganizationId);
        StartDate = UnixTimestamp.Create(startDate);
        EndDate = endDate is not null ? UnixTimestamp.Create(endDate.Value) : null;
        Type = type;
    }
}

public class CreateTransferAgreementProposalCommandResult
{
    public Guid Id { get; }
    public string SenderOrganizationName { get; }
    public string SenderOrganizationTin { get; }
    public string? ReceiverOrganizationTin { get; }
    public long StartDate { get; }
    public long? EndDate { get; }
    public TransferAgreementType Type { get; }

    public CreateTransferAgreementProposalCommandResult(Guid id, string senderOrganizationName, string senderOrganizationTin,
        string? receiverOrganizationTin, long startDate, long? endDate, TransferAgreementType type)
    {
        Id = id;
        SenderOrganizationName = senderOrganizationName;
        SenderOrganizationTin = senderOrganizationTin;
        ReceiverOrganizationTin = receiverOrganizationTin;
        StartDate = startDate;
        EndDate = endDate;
        Type = type;
    }
}

public class CreateTransferAgreementProposalCommandHandler : IRequestHandler<CreateTransferAgreementProposalCommand,
    CreateTransferAgreementProposalCommandResult>
{
    private readonly IdentityDescriptor _identityDescriptor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthorizationClient _authorizationClient;
    private readonly ILogger<CreateTransferAgreementProposalCommandHandler> _logger;

    public CreateTransferAgreementProposalCommandHandler(IdentityDescriptor identityDescriptor, IUnitOfWork unitOfWork, IAuthorizationClient authorizationClient, ILogger<CreateTransferAgreementProposalCommandHandler> logger)
    {
        _identityDescriptor = identityDescriptor;
        _unitOfWork = unitOfWork;
        _authorizationClient = authorizationClient;
        _logger = logger;
    }

    public async Task<CreateTransferAgreementProposalCommandResult> Handle(CreateTransferAgreementProposalCommand command,
        CancellationToken cancellationToken)
    {
        var (organizationId, organizationTin, organizationName) = await GetOrganizationOnBehalfOf(command);

        if (command.ReceiverOrganizationTin != Tin.Empty() && command.ReceiverOrganizationTin.Value.Equals(organizationTin.Value))
        {
            throw new SameSenderAndReceiverException();
        }

        var newProposal = new TransferAgreementProposal
        {
            Id = Guid.NewGuid(),
            SenderCompanyId = organizationId,
            SenderCompanyTin = organizationTin,
            SenderCompanyName = organizationName,
            ReceiverCompanyTin = command.ReceiverOrganizationTin,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Type = command.Type,
            IsTrial = _identityDescriptor.IsTrial()
        };

        if (command.ReceiverOrganizationTin != Tin.Empty())
        {
            var hasConflict = await _unitOfWork.TransferAgreementRepo.HasDateOverlap(newProposal, cancellationToken);
            if (hasConflict)
            {
                throw new TransferAgreementConflictException();
            }
        }

        await _unitOfWork.TransferAgreementProposalRepo.AddTransferAgreementProposal(newProposal, cancellationToken);

        await AppendToActivityLog(_identityDescriptor, newProposal, ActivityLogEntry.ActionTypeEnum.Created);

        await _unitOfWork.SaveAsync();

        return new CreateTransferAgreementProposalCommandResult(newProposal.Id, newProposal.SenderCompanyName.Value,
            newProposal.SenderCompanyTin.Value, newProposal.ReceiverCompanyTin.IsEmpty() ? null : newProposal.ReceiverCompanyTin.Value,
            newProposal.StartDate.EpochSeconds, newProposal.EndDate?.EpochSeconds, newProposal.Type);
    }

    private async Task<(OrganizationId organizationId, Tin organizationTin, OrganizationName organizationName)> GetOrganizationOnBehalfOf(CreateTransferAgreementProposalCommand command)
    {
        OrganizationId organizationId;
        Tin organizationTin;
        OrganizationName organizationName;

        if (_identityDescriptor.OrganizationId == command.SenderOrganizationId.Value)
        {
            organizationId = OrganizationId.Create(_identityDescriptor.OrganizationId);
            organizationTin = Tin.Create(_identityDescriptor.OrganizationCvr!);
            organizationName = OrganizationName.Create(_identityDescriptor.OrganizationName);
        }
        else
        {
            _logger.LogInformation("Creating transfer agreement proposal on behalf of another organization.");
            var consents = await _authorizationClient.GetConsentsAsync();

            if (consents == null)
            {
                throw new Exception("Failed to get consents from authorization.");
            }

            (organizationId, organizationTin, organizationName) = consents.GetCurrentOrganizationBehalfOf(command.SenderOrganizationId.Value);
        }

        return (organizationId, organizationTin, organizationName);
    }

    private async Task AppendToActivityLog(IdentityDescriptor identity, TransferAgreementProposal proposal,
        ActivityLogEntry.ActionTypeEnum actionType)
    {
        await _unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
            actorId: identity.Subject,
            actorType: ActivityLogEntry.ActorTypeEnum.User,
            actorName: identity.Name,
            organizationTin: identity.OrganizationCvr!,
            organizationName: identity.OrganizationName,
            otherOrganizationTin: proposal.ReceiverCompanyTin?.Value ?? string.Empty,
            otherOrganizationName: string.Empty,
            entityType: ActivityLogEntry.EntityTypeEnum.TransferAgreementProposal,
            actionType: actionType,
            entityId: proposal.Id.ToString())
        );
    }
}
