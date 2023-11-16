using System;
using System.Threading.Tasks;
using API.Shared.Data;
using API.Transfer.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Transfer.Api.Repository;

public interface ITransferAgreementProposalRepository
{
    Task AddTransferAgreementProposal(TransferAgreementProposal proposal);
    Task DeleteTransferAgreementProposal(Guid id);
    Task DeleteOldTransferAgreementProposals(DateTimeOffset olderThan);
    Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposal(Guid id);
}

public class TransferAgreementProposalRepository : ITransferAgreementProposalRepository
{
    private readonly ApplicationDbContext context;

    public TransferAgreementProposalRepository(ApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task AddTransferAgreementProposal(TransferAgreementProposal proposal)
    {
        context.TransferAgreementProposals.Add(proposal);
        await context.SaveChangesAsync();
    }

    public async Task DeleteTransferAgreementProposal(Guid id)
    {
        var invitation = await context.TransferAgreementProposals.FindAsync(id);

        if (invitation != null)
        {
            context.TransferAgreementProposals.Remove(invitation);
            await context.SaveChangesAsync();
        }
    }

    public Task DeleteOldTransferAgreementProposals(DateTimeOffset olderThan)
    {
        throw new NotImplementedException();
    }

    public async Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposal(Guid id)
    {
        var invitation = await context.TransferAgreementProposals
            .FirstOrDefaultAsync(i => i.CreatedAt > DateTimeOffset.UtcNow.AddDays(-14) && i.Id == id);

        return invitation;
    }
}
