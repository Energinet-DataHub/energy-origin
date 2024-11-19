using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Exceptions;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;

namespace API.Transfer.Api._Features_;

public class CreateTransferAgreementProposalCommand : IRequest<CreateTransferAgreementProposalCommandResult>
{
    public OrganizationId SenderOrganizationId { get; }
    public Tin SenderOrganizationTin { get; }
    public OrganizationName SenderOrganizationName { get; }
    public Tin ReceiverOrganizationTin { get; }
    public UnixTimestamp StartDate { get; }
    public UnixTimestamp? EndDate { get; }
    public TransferAgreementType Type { get; }

    public CreateTransferAgreementProposalCommand(Guid senderOrganizationId, string? senderOrganizationTin, string? senderOrganizationName,
        string? receiverOrganizationTin, long startDate, long? endDate, TransferAgreementType type)
    {
        ReceiverOrganizationTin = receiverOrganizationTin is not null ? Tin.Create(receiverOrganizationTin) : Tin.Empty();
        SenderOrganizationId = OrganizationId.Create(senderOrganizationId);
        SenderOrganizationTin = senderOrganizationTin is not null ? Tin.Create(senderOrganizationTin) : Tin.Empty();
        SenderOrganizationName = senderOrganizationName is not null ? OrganizationName.Create(senderOrganizationName) : OrganizationName.Empty();
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

    public CreateTransferAgreementProposalCommandHandler(IdentityDescriptor identityDescriptor, IUnitOfWork unitOfWork)
    {
        _identityDescriptor = identityDescriptor;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateTransferAgreementProposalCommandResult> Handle(CreateTransferAgreementProposalCommand command,
        CancellationToken cancellationToken = default)
    {

        if (command.ReceiverOrganizationTin != Tin.Empty() && command.ReceiverOrganizationTin.Value.Equals(_identityDescriptor.OrganizationCvr))
        {
            throw new SameSenderAndReceiverException();
        }

        var newProposal = new TransferAgreementProposal
        {
            Id = Guid.NewGuid(),
            SenderCompanyId = command.SenderOrganizationId,
            SenderCompanyTin = command.SenderOrganizationTin,
            SenderCompanyName = command.SenderOrganizationName,
            ReceiverCompanyTin = command.ReceiverOrganizationTin,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Type = command.Type
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
