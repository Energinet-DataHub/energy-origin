using System;
using System.Threading;
using System.Threading.Tasks;
using API.Transfer.Api.Exceptions;
using API.UnitOfWork;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Exceptions;
using EnergyOrigin.TokenValidation.b2c;
using MediatR;

namespace API.Transfer.Api._Features_;

public class EditTransferAgreementEndDateCommand : IRequest<EditTransferAgreementEndDateCommandResult>
{
    public Guid TransferAgreementId { get; set; }
    public long? EndDate { get; }

    public EditTransferAgreementEndDateCommand(Guid transferAgreementId, long? endDate)
    {
        TransferAgreementId = transferAgreementId;
        EndDate = endDate;
    }
}

public class EditTransferAgreementEndDateCommandResult
{
    public TransferAgreement TransferAgreement { get; }

    public EditTransferAgreementEndDateCommandResult(TransferAgreement transferAgreement)
    {
        TransferAgreement = transferAgreement;
    }
}

public class EditTransferAgreementEndDateCommandHandler : IRequestHandler<EditTransferAgreementEndDateCommand,
    EditTransferAgreementEndDateCommandResult>
{
    private readonly IdentityDescriptor _identityDescriptor;
    private readonly AccessDescriptor _accessDescriptor;
    private readonly IUnitOfWork _unitOfWork;

    public EditTransferAgreementEndDateCommandHandler(IdentityDescriptor identityDescriptor, AccessDescriptor accessDescriptor, IUnitOfWork unitOfWork)
    {
        _identityDescriptor = identityDescriptor;
        _accessDescriptor = accessDescriptor;
        _unitOfWork = unitOfWork;
    }

    public async Task<EditTransferAgreementEndDateCommandResult> Handle(EditTransferAgreementEndDateCommand request, CancellationToken cancellationToken)
    {
        var endDate = request.EndDate.HasValue
            ? UnixTimestamp.Create(request.EndDate.Value)
            : null;

        var transferAgreement = await _unitOfWork.TransferAgreementRepo.GetTransferAgreement(request.TransferAgreementId, cancellationToken);

        if (!IsAuthorizedToSender(transferAgreement) && !IsAuthorizedToReceiver(transferAgreement))
        {
            throw new ForbiddenException();
        }

        if (transferAgreement.EndDate != null && transferAgreement.EndDate < UnixTimestamp.Now())
        {
            throw new TransferAgreementExpiredException();
        }

        var overlapQuery = new TransferAgreement
        {
            Id = transferAgreement.Id,
            StartDate = transferAgreement.StartDate,
            EndDate = endDate,
            SenderId = transferAgreement.SenderId,
            ReceiverTin = transferAgreement.ReceiverTin
        };

        if (await _unitOfWork.TransferAgreementRepo.HasDateOverlap(overlapQuery, cancellationToken))
        {
            throw new TransferAgreementConflictException();
        }

        transferAgreement.EndDate = endDate;

        await _unitOfWork.SaveAsync();

        return new EditTransferAgreementEndDateCommandResult(transferAgreement);
    }

    private bool IsAuthorizedToSender(TransferAgreement transferAgreement)
    {
        return _accessDescriptor.IsAuthorizedToOrganization(transferAgreement.SenderId.Value);
    }

    private bool IsAuthorizedToReceiver(TransferAgreement transferAgreement)
    {
        return transferAgreement.ReceiverId is not null && _accessDescriptor.IsAuthorizedToOrganization(transferAgreement.ReceiverId.Value);
    }
}
