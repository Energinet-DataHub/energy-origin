using System;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace API.Transfer.Api.Repository;

public interface ITransferAgreementProposalRepository
{
    Task AddTransferAgreementProposal(TransferAgreementProposal proposal, CancellationToken cancellationToken);
    Task DeleteTransferAgreementProposal(Guid id, CancellationToken cancellationToken);
    Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposal(Guid id, CancellationToken cancellationToken);
    Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposalAsNoTracking(Guid id, CancellationToken cancellationToken);
}

public class TransferAgreementProposalRepository(ApplicationDbContext context) : ITransferAgreementProposalRepository
{
    public async Task AddTransferAgreementProposal(TransferAgreementProposal proposal, CancellationToken cancellationToken)
    {
        context.TransferAgreementProposals.Add(proposal);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTransferAgreementProposal(Guid id, CancellationToken cancellationToken)
    {
        var proposal = await context.TransferAgreementProposals.FindAsync(id, cancellationToken);

        if (proposal != null)
        {
            context.TransferAgreementProposals.Remove(proposal);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposal(Guid id, CancellationToken cancellationToken)
    {
        var expireTime = UnixTimestamp.Now().Add(TimeSpan.FromDays(-14));
        var proposal = await context.TransferAgreementProposals
            .FirstOrDefaultAsync(i => i.CreatedAt > expireTime && i.Id == id, cancellationToken);

        return proposal;
    }

    public async Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposalAsNoTracking(Guid id, CancellationToken cancellationToken)
    {
        var proposal = await context.TransferAgreementProposals
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.CreatedAt > UnixTimestamp.Now().Add(TimeSpan.FromDays(-14)) && i.Id == id, cancellationToken);

        return proposal;
    }
}
