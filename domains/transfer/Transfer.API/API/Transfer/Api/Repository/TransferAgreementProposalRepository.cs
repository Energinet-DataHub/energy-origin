using System;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace API.Transfer.Api.Repository;

public interface ITransferAgreementProposalRepository
{
    Task AddTransferAgreementProposal(TransferAgreementProposal proposal);
    Task DeleteTransferAgreementProposal(Guid id);
    Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposal(Guid id);
    Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposalAsNoTracking(Guid id);
}

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
        var expireTime = UnixTimestamp.Now().Add(TimeSpan.FromDays(-14));
        var proposal = await context.TransferAgreementProposals
            .FirstOrDefaultAsync(i => i.CreatedAt > expireTime && i.Id == id);

        return proposal;
    }

    public async Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposalAsNoTracking(Guid id)
    {
        var proposal = await context.TransferAgreementProposals
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.CreatedAt > UnixTimestamp.Now().Add(TimeSpan.FromDays(-14)) && i.Id == id);

        return proposal;
    }
}
