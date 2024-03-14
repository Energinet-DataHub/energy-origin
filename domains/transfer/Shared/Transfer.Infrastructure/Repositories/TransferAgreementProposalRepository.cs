using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Transfer.Domain.Entities;
using Transfer.Application.Repositories;

namespace DataContext.Repositories;

public class TransferAgreementProposalRepository(ApplicationDbContext context) : ITransferAgreementProposalRepository
{
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
