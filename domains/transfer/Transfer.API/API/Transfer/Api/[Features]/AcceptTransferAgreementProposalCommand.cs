using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Exceptions;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.ActivityLog.DataContext;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;
using ProjectOriginClients;

namespace API.Transfer.Api._Features_;

public class AcceptTransferAgreementProposalCommand : IRequest<AcceptTransferAgreementProposalCommandResult>
{
    public Guid TransferAgreementProposalId { get; }
    public Guid ReceiverOrganizationId { get; }
    public Tin ReceiverOrganizationTin { get; }
    public OrganizationName ReceiverOrganizationName { get; }

    public AcceptTransferAgreementProposalCommand(Guid transferAgreementProposalId, Guid receiverOrganizationId, string? receiverOrganizationTin,
        string? receiverOrganizationName)
    {
        TransferAgreementProposalId = transferAgreementProposalId;
        ReceiverOrganizationId = receiverOrganizationId;
        ReceiverOrganizationTin = receiverOrganizationTin is not null ? Tin.Create(receiverOrganizationTin) : Tin.Empty();
        ReceiverOrganizationName =
            receiverOrganizationName is not null ? OrganizationName.Create(receiverOrganizationName) : OrganizationName.Empty();
    }
}

public class AcceptTransferAgreementProposalCommandResult
{
    public Guid TransferAgreementId { get; }
    public string SenderName { get; }
    public string SenderTin { get; }
    public string ReceiverTin { get; }
    public long StartDate { get; }
    public long? EndDate { get; }
    public TransferAgreementType Type { get; }

    public AcceptTransferAgreementProposalCommandResult(Guid transferAgreementId, string senderName, string senderTin, string receiverTin,
        long startDate, long? endDate, TransferAgreementType type)
    {
        TransferAgreementId = transferAgreementId;
        SenderName = senderName;
        SenderTin = senderTin;
        ReceiverTin = receiverTin;
        StartDate = startDate;
        EndDate = endDate;
        Type = type;
    }
}

public class AcceptTransferAgreementProposalCommandHandler : IRequestHandler<AcceptTransferAgreementProposalCommand,
    AcceptTransferAgreementProposalCommandResult>
{
    private readonly IProjectOriginWalletClient _walletClient;
    private readonly IdentityDescriptor _identityDescriptor;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptTransferAgreementProposalCommandHandler(IdentityDescriptor identityDescriptor, IUnitOfWork unitOfWork,
        IProjectOriginWalletClient walletClient)
    {
        _identityDescriptor = identityDescriptor;
        _unitOfWork = unitOfWork;
        _walletClient = walletClient;
    }

    public async Task<AcceptTransferAgreementProposalCommandResult> Handle(AcceptTransferAgreementProposalCommand command,
        CancellationToken cancellationToken)
    {
        if (command.TransferAgreementProposalId == Guid.Empty)
        {
            throw new BusinessException("Must set TransferAgreementProposalId");
        }

        var proposal =
            await _unitOfWork.TransferAgreementProposalRepo.GetNonExpiredTransferAgreementProposalAsNoTracking(command.TransferAgreementProposalId,
                cancellationToken);
        if (proposal == null)
        {
            throw new EntityNotFoundException(command.TransferAgreementProposalId, typeof(TransferAgreementProposal));
        }

        if (proposal.ReceiverCompanyTin is not null && proposal.ReceiverCompanyTin != command.ReceiverOrganizationTin)
        {
            throw new BusinessException("Only the receiver company can accept this Transfer Agreement Proposal");
        }

        if (proposal.EndDate is not null && proposal.EndDate < UnixTimestamp.Now())
        {
            throw new BusinessException("This proposal has run out");
        }

        proposal.ReceiverCompanyTin = command.ReceiverOrganizationTin;

        var taRepo = _unitOfWork.TransferAgreementRepo;

        var hasConflict = await taRepo.HasDateOverlap(proposal, cancellationToken);
        if (hasConflict)
        {
            throw new TransferAgreementConflictException();
        }


        var receiverOrganizationId = command.ReceiverOrganizationId;
        var wallets = await _walletClient.GetWallets(receiverOrganizationId, CancellationToken.None);

        var walletId = wallets.Result.FirstOrDefault()?.Id;
        if (walletId == null)
        {
            var createWalletResponse = await _walletClient.CreateWallet(receiverOrganizationId, CancellationToken.None);

            if (createWalletResponse == null)
            {
                throw new Exception("Failed to create wallet.");
            }

            walletId = createWalletResponse.WalletId;
        }

        var walletEndpoint = await _walletClient.CreateWalletEndpoint(receiverOrganizationId, walletId.Value, CancellationToken.None);

        var externalEndpoint =
            await _walletClient.CreateExternalEndpoint(proposal.SenderCompanyId.Value, walletEndpoint, proposal.ReceiverCompanyTin.Value,
                CancellationToken.None);

        var transferAgreement = new TransferAgreement
        {
            StartDate = proposal.StartDate,
            EndDate = proposal.EndDate,
            SenderId = proposal.SenderCompanyId,
            SenderName = proposal.SenderCompanyName,
            SenderTin = proposal.SenderCompanyTin,
            ReceiverName = command.ReceiverOrganizationName,
            ReceiverTin = command.ReceiverOrganizationTin,
            ReceiverReference = externalEndpoint.ReceiverId,
            Type = proposal.Type
        };

        var result = await taRepo.AddTransferAgreementAndDeleteProposal(transferAgreement, command.TransferAgreementProposalId, cancellationToken);

        await AppendProposalAcceptedToActivityLog(_identityDescriptor, result, proposal);

        await _unitOfWork.SaveAsync();

        return new AcceptTransferAgreementProposalCommandResult(result.Id, result.SenderName.Value, result.SenderTin.Value, result.ReceiverTin.Value,
            result.StartDate.EpochSeconds, result.EndDate?.EpochSeconds, result.Type);
    }

    private async Task AppendProposalAcceptedToActivityLog(IdentityDescriptor identity, TransferAgreement result, TransferAgreementProposal proposal)
    {
        // Receiver entry
        await _unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
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
        await _unitOfWork.ActivityLogEntryRepo.AddActivityLogEntryAsync(ActivityLogEntry.Create(
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
}