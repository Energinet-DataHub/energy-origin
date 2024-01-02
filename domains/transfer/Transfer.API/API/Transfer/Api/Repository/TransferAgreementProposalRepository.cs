using System;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
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
