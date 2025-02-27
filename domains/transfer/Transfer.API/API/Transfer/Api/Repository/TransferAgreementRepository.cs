using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using EnergyOrigin.Setup.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace API.Transfer.Api.Repository;

public class TransferAgreementRepository(ApplicationDbContext context) : ITransferAgreementRepository
{
    public async Task<TransferAgreement> AddTransferAgreementAndDeleteProposal(TransferAgreement newTransferAgreement, Guid proposalId, CancellationToken cancellationToken)
    {
        var agreements = await context.TransferAgreements.Where(t =>
                t.SenderId == newTransferAgreement.SenderId)
            .ToListAsync(cancellationToken);
        var transferAgreementNumber = agreements.Any() ? agreements.Max(ta => ta.TransferAgreementNumber) + 1 : 0;
        newTransferAgreement.TransferAgreementNumber = transferAgreementNumber;

        await context.TransferAgreements.AddAsync(newTransferAgreement, cancellationToken);

        var proposal = await context.TransferAgreementProposals.FindAsync(proposalId, cancellationToken);
        if (proposal != null)
        {
            context.TransferAgreementProposals.Remove(proposal);
        }

        return newTransferAgreement;
    }

    public async Task<TransferAgreement> AddTransferAgreement(TransferAgreement newTransferAgreement, CancellationToken cancellationToken)
    {
        var agreements = await context.TransferAgreements.Where(t =>
                t.SenderId == newTransferAgreement.SenderId)
            .ToListAsync(cancellationToken);
        var transferAgreementNumber = agreements.Any() ? agreements.Max(ta => ta.TransferAgreementNumber) + 1 : 0;
        newTransferAgreement.TransferAgreementNumber = transferAgreementNumber;

        await context.TransferAgreements.AddAsync(newTransferAgreement, cancellationToken);

        return newTransferAgreement;
    }


    public async Task<List<TransferAgreement>> GetTransferAgreementsList(Guid organizationId, string receiverTin, CancellationToken cancellationToken)
    {
        var tin = Tin.Create(receiverTin);
        var orgId = OrganizationId.Create(organizationId);
        return await context.TransferAgreements
            .Where(ta => ta.SenderId == orgId || ta.ReceiverTin == tin || ta.ReceiverId == orgId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TransferAgreement>> GetTransferAgreementsList(IList<Guid> organizationIds, CancellationToken cancellationToken)
    {
        var orgIds = organizationIds.Select(OrganizationId.Create).ToList();
        return await context.TransferAgreements
            .Where(ta => orgIds.Contains(ta.SenderId) || (ta.ReceiverId != null && orgIds.Contains(ta.ReceiverId)))
            .ToListAsync(cancellationToken);
    }

    public async Task<TransferAgreement?> GetTransferAgreement(Guid id, string subject, string tin, CancellationToken cancellationToken)
    {
        var receiverTin = Tin.Create(tin);
        var organizationId = OrganizationId.Create(Guid.Parse(subject));
        return await context
            .TransferAgreements
            .Where(agreement => agreement.Id == id && (agreement.SenderId == organizationId || agreement.ReceiverTin == receiverTin))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TransferAgreement> GetTransferAgreement(Guid id, CancellationToken cancellationToken)
    {
        var transferAgreement = await context
            .TransferAgreements
            .Where(agreement => agreement.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (transferAgreement is null)
        {
            throw new EntityNotFoundException(id, typeof(TransferAgreement));
        }

        return transferAgreement;
    }

    public async Task<bool> HasDateOverlap(TransferAgreement transferAgreement, CancellationToken cancellationToken)
    {
        var overlappingAgreements = await context.TransferAgreements.AsNoTracking()
            .Where(t => t.SenderId == transferAgreement.SenderId &&
                        t.ReceiverTin == transferAgreement.ReceiverTin &&
                        t.Id != transferAgreement.Id)
            .ToListAsync(cancellationToken);

        return overlappingAgreements.Any(a =>
            IsOverlappingTransferAgreement(a, transferAgreement.StartDate.ToDateTimeOffset(), transferAgreement.EndDate?.ToDateTimeOffset())
        );
    }

    private static bool IsOverlappingTransferAgreement(TransferAgreement transferAgreement, DateTimeOffset startDate, DateTimeOffset? endDate)
    {
        return !(startDate >= transferAgreement.EndDate?.ToDateTimeOffset() || endDate <= transferAgreement.StartDate.ToDateTimeOffset());
    }

    public async Task<bool> HasDateOverlap(TransferAgreementProposal proposal, CancellationToken cancellationToken)
    {
        var proposalReceiverCompanyTin = proposal.ReceiverCompanyTin;
        var overlappingAgreements = await context.TransferAgreements
            .Where(t => t.SenderId == proposal.SenderCompanyId && t.ReceiverTin == proposalReceiverCompanyTin)
            .ToListAsync(cancellationToken);

        return overlappingAgreements.Any(a => IsOverlappingTransferAgreement(a, proposal.StartDate.ToDateTimeOffset(), proposal.EndDate?.ToDateTimeOffset()));
    }

    public async Task<List<TransferAgreementProposal>> GetTransferAgreementProposals(Guid organizationId, CancellationToken cancellationToken)
    {
        var orgId = OrganizationId.Create(organizationId);
        return await context.TransferAgreementProposals
            .Where(p => p.SenderCompanyId == orgId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TransferAgreementProposal>> GetTransferAgreementProposals(IList<Guid> organizationIds, CancellationToken cancellationToken)
    {
        var orgIds = organizationIds.Select(OrganizationId.Create).ToList();
        return await context.TransferAgreementProposals
            .Where(p => orgIds.Contains(p.SenderCompanyId))
            .ToListAsync(cancellationToken);
    }
}
