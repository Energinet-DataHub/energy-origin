using System;
using System.Threading.Tasks;
using API.Shared.Data;
using API.Transfer.Api.Models;
using API.Transfer.Api.Services;
using MassTransitContracts.Contracts;
using Microsoft.EntityFrameworkCore;

namespace API.Transfer.Api.Repository;

public interface ITransferAgreementProposalRepository
{
    Task AddTransferAgreementProposal(TransferAgreementProposal proposal);
    Task DeleteTransferAgreementProposal(Guid id);
    Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposal(Guid id);
    Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposalAsNoTracking(Guid id);
}

public class TransferAgreementProposalRepository : ITransferAgreementProposalRepository
{
    private readonly ApplicationDbContext context;
    private readonly IUserActivityEventPublisher userActivityEventPublisher;


    public TransferAgreementProposalRepository(ApplicationDbContext context, IUserActivityEventPublisher userActivityEventPublisher)
    {
        this.context = context;
        this.userActivityEventPublisher = userActivityEventPublisher;
    }

    public async Task AddTransferAgreementProposal(TransferAgreementProposal proposal)
    {
        context.TransferAgreementProposals.Add(proposal);
        await context.SaveChangesAsync();
        var userActivityEvent = new UserActivityEvent
        {
            Id = proposal.Id,
            ActorId = proposal.SenderCompanyId, // Assuming 'CreatedBy' is a property of your proposal
            EntityType = EntityType.TransferAgreements,
            ActivityDate = DateTimeOffset.UtcNow,
            OrganizationId = proposal.SenderCompanyId, // Replace with actual property
            Tin = proposal.SenderCompanyTin, // Replace with actual value
            OrganizationName = proposal.SenderCompanyName // Replace with actual value
        };

        await userActivityEventPublisher.PublishUserActivityEvent(userActivityEvent);
    }

    public async Task DeleteTransferAgreementProposal(Guid id)
    {
        var proposal = await context.TransferAgreementProposals.FindAsync(id);

        if (proposal != null)
        {
            context.TransferAgreementProposals.Remove(proposal);
            await context.SaveChangesAsync();
        }
    }

    public async Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposal(Guid id)
    {
        var proposal = await context.TransferAgreementProposals
            .FirstOrDefaultAsync(i => i.CreatedAt > DateTimeOffset.UtcNow.AddDays(-14) && i.Id == id);

        return proposal;
    }

    public async Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposalAsNoTracking(Guid id)
    {
        var proposal = await context.TransferAgreementProposals
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.CreatedAt > DateTimeOffset.UtcNow.AddDays(-14) && i.Id == id);

        return proposal;
    }
}
