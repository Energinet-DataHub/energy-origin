using System;
using System.Threading;
using System.Threading.Tasks;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Setup.Exceptions;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;

namespace API.Transfer.Api._Features_;

public class DeleteTransferAgreementProposalCommand : IRequest<DeleteTransferAgreementProposalCommandResult>
{
    public Guid TransferAgreementProposalId { get; }

    public DeleteTransferAgreementProposalCommand(Guid transferAgreementProposalId)
    {
        TransferAgreementProposalId = transferAgreementProposalId;
    }
}

public class DeleteTransferAgreementProposalCommandResult
{
}

public class DeleteTransferAgreementProposalCommandHandler : IRequestHandler<DeleteTransferAgreementProposalCommand,
    DeleteTransferAgreementProposalCommandResult>
{
    private readonly IdentityDescriptor _identityDescriptor;
    private readonly AccessDescriptor _accessDescriptor;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTransferAgreementProposalCommandHandler(IdentityDescriptor identityDescriptor, AccessDescriptor accessDescriptor, IUnitOfWork unitOfWork)
    {
        _identityDescriptor = identityDescriptor;
        _accessDescriptor = accessDescriptor;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteTransferAgreementProposalCommandResult> Handle(DeleteTransferAgreementProposalCommand request,
        CancellationToken cancellationToken)
    {
        var proposalId = request.TransferAgreementProposalId;
        var proposal = await _unitOfWork.TransferAgreementProposalRepo.GetNonExpiredTransferAgreementProposal(proposalId, cancellationToken);

        if (proposal is null)
        {
            throw new EntityNotFoundException(proposalId, typeof(TransferAgreementProposal));
        }

        if (!IsAuthorizedToSender(proposal) && !IsReceiver(proposal))
        {
            throw new ForbiddenException();
        }

        await _unitOfWork.TransferAgreementProposalRepo.DeleteTransferAgreementProposal(proposalId, cancellationToken);
        await AppendToActivityLog(_identityDescriptor, proposal, ActivityLogEntry.ActionTypeEnum.Declined);

        await _unitOfWork.SaveAsync();

        return new DeleteTransferAgreementProposalCommandResult();
    }

    private bool IsAuthorizedToSender(TransferAgreementProposal proposal)
    {
        return _accessDescriptor.IsAuthorizedToOrganization(proposal.SenderCompanyId.Value);
    }

    private bool IsReceiver(TransferAgreementProposal proposal)
    {
        return proposal.ReceiverCompanyTin != null && _identityDescriptor.OrganizationCvr == proposal.ReceiverCompanyTin.Value;
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
