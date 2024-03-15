using EnergyOrigin.ActivityLog.API;
using EnergyOrigin.ActivityLog.DataContext;
using Transfer.Application.Exceptions;
using Transfer.Application.Repositories;
using Transfer.Domain.Entities;

namespace Transfer.Application.Commands;

public class CreateTransferAgreementProposalCommand : ICommand<CreateTransferAgreementProposalCommandResponse>
{
    public long StartDate { get; }
    public long? EndDate { get; }
    public string? ReceiverTin { get; }

    public CreateTransferAgreementProposalCommand(long startDate, long? endDate, string? receiverTin)
    {
        StartDate = startDate;
        EndDate = endDate;
        ReceiverTin = receiverTin;
    }
}

public class CreateTransferAgreementProposalCommandResponse : ICommandResponse
{
    public Guid Id { get; }
    public string SenderCompanyName { get; }
    public string? ReceiverCompanyTin { get; }
    public long StartDate { get; }
    public long? EndDate { get; }

    public CreateTransferAgreementProposalCommandResponse(Guid id, string senderCompanyName, string? receiverCompanyTin, long startDate, long? endDate)
    {
        Id = id;
        SenderCompanyName = senderCompanyName;
        ReceiverCompanyTin = receiverCompanyTin;
        StartDate = startDate;
        EndDate = endDate;
    }
}

internal class CreateTransferAgreementProposalCommandHandler : ICommandHandler<CreateTransferAgreementProposalCommand, CreateTransferAgreementProposalCommandResponse>
{
    private readonly ITransferAgreementRepository transferAgreementRepository;
    private readonly ITransferAgreementProposalRepository transferAgreementProposalRepository;
    private readonly IActivityLogEntryRepository activityLogEntryRepository;
    private readonly IUserContext userContext;

    public CreateTransferAgreementProposalCommandHandler(ITransferAgreementRepository transferAgreementRepository,
        ITransferAgreementProposalRepository transferAgreementProposalRepository, IActivityLogEntryRepository activityLogEntryRepository, IUserContext userContext)
    {
        this.transferAgreementRepository = transferAgreementRepository;
        this.transferAgreementProposalRepository = transferAgreementProposalRepository;
        this.activityLogEntryRepository = activityLogEntryRepository;
        this.userContext = userContext;
    }

    public async Task<CreateTransferAgreementProposalCommandResponse> Handle(CreateTransferAgreementProposalCommand request, CancellationToken cancellationToken)
    {
        var newProposal = new TransferAgreementProposal
        {
            SenderCompanyId = userContext.OrganizationId,
            SenderCompanyTin = userContext.OrganizationTin,
            SenderCompanyName = userContext.OrganizationName,
            Id = Guid.NewGuid(),
            ReceiverCompanyTin = request.ReceiverTin,
            StartDate = DateTimeOffset.FromUnixTimeSeconds(request.StartDate),
            EndDate = request.EndDate == null ? null : DateTimeOffset.FromUnixTimeSeconds(request.EndDate.Value)
        };

        if (request.ReceiverTin != null)
        {
            var hasConflict = await transferAgreementRepository.HasDateOverlap(newProposal);

            if (hasConflict)
            {
                throw new TransferAgreementOverlapsException();
            }
        }

        await transferAgreementProposalRepository.AddTransferAgreementProposal(newProposal);

        await AppendToActivityLog(newProposal, ActivityLogEntry.ActionTypeEnum.Created);

        return new CreateTransferAgreementProposalCommandResponse(
            newProposal.Id,
            newProposal.SenderCompanyName,
            newProposal.ReceiverCompanyTin,
            newProposal.StartDate.ToUnixTimeSeconds(),
            newProposal.EndDate?.ToUnixTimeSeconds());
    }

    private async Task AppendToActivityLog(TransferAgreementProposal proposal, ActivityLogEntry.ActionTypeEnum actionType)
    {
        await activityLogEntryRepository.AddActivityLogEntryAsync(ActivityLogEntry.Create(
            actorId: userContext.Subject,
            actorType: ActivityLogEntry.ActorTypeEnum.User,
            actorName: userContext.Name,
            organizationTin: userContext.OrganizationTin,
            organizationName: userContext.OrganizationName,
            otherOrganizationTin: proposal.ReceiverCompanyTin ?? string.Empty,
            otherOrganizationName: string.Empty,
            entityType: ActivityLogEntry.EntityTypeEnum.TransferAgreementProposal,
            actionType: actionType,
            entityId: proposal.Id.ToString())
        );
    }

}
