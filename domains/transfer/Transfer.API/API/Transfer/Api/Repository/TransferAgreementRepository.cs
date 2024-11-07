using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContext;
using DataContext.Models;
using EnergyOrigin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace API.Transfer.Api.Repository;

public class TransferAgreementRepository(ApplicationDbContext context) : ITransferAgreementRepository
{
    public async Task<TransferAgreement> AddTransferAgreementToDb(TransferAgreement transferAgreement)
    {
        var agreements = await context.TransferAgreements.Where(t =>
                t.SenderId == transferAgreement.SenderId)
            .ToListAsync();

        var transferAgreementNumber = agreements.Any() ? agreements.Max(ta => ta.TransferAgreementNumber) + 1 : 0;
        transferAgreement.TransferAgreementNumber = transferAgreementNumber;
        context.TransferAgreements.Add(transferAgreement);

        return transferAgreement;
    }

    public async Task<TransferAgreement> AddTransferAgreementAndDeleteProposal(TransferAgreement newTransferAgreement, Guid proposalId)
    {
        var agreements = await context.TransferAgreements.Where(t =>
                t.SenderId == newTransferAgreement.SenderId)
            .ToListAsync();
        var transferAgreementNumber = agreements.Any() ? agreements.Max(ta => ta.TransferAgreementNumber) + 1 : 0;
        newTransferAgreement.TransferAgreementNumber = transferAgreementNumber;

        await context.TransferAgreements.AddAsync(newTransferAgreement);

        var proposal = await context.TransferAgreementProposals.FindAsync(proposalId);
        if (proposal != null)
        {
            context.TransferAgreementProposals.Remove(proposal);
        }

        return newTransferAgreement;
    }

    public async Task<List<TransferAgreement>> GetTransferAgreementsList(Guid organizationId, string receiverTin)
    {
        var tin = Tin.Create(receiverTin);
        var orgId = OrganizationId.Create(organizationId);
        return await context.TransferAgreements
            .Where(ta => ta.SenderId == orgId || ta.ReceiverTin == tin)
            .ToListAsync();
    }

    public Task<List<TransferAgreement>> GetAllTransferAgreements()
    {
        return context.TransferAgreements.ToListAsync();
    }

    public async Task<TransferAgreement?> GetTransferAgreement(Guid id, string subject, string tin)
    {
        var receiverTin = Tin.Create(tin);
        var organizationId = OrganizationId.Create(Guid.Parse(subject));
        return await context
            .TransferAgreements
            .Where(agreement => agreement.Id == id && (agreement.SenderId == organizationId || agreement.ReceiverTin == receiverTin))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasDateOverlap(TransferAgreement transferAgreement)
    {
        var overlappingAgreements = await context.TransferAgreements
            .Where(t => t.SenderId == transferAgreement.SenderId &&
                        t.ReceiverTin == transferAgreement.ReceiverTin &&
                        t.Id != transferAgreement.Id)
            .ToListAsync();

        return overlappingAgreements.Any(a =>
            IsOverlappingTransferAgreement(a, transferAgreement.StartDate.ToDateTimeOffset(), transferAgreement.EndDate?.ToDateTimeOffset())
        );
    }

    private static bool IsOverlappingTransferAgreement(TransferAgreement transferAgreement, DateTimeOffset startDate, DateTimeOffset? endDate)
    {
        return !(startDate >= transferAgreement.EndDate?.ToDateTimeOffset() || endDate <= transferAgreement.StartDate.ToDateTimeOffset());
    }

    public async Task<bool> HasDateOverlap(TransferAgreementProposal proposal)
    {
        var proposalReceiverCompanyTin = proposal.ReceiverCompanyTin;
        var overlappingAgreements = await context.TransferAgreements
            .Where(t => t.SenderId == proposal.SenderCompanyId && t.ReceiverTin == proposalReceiverCompanyTin)
            .ToListAsync();

        return overlappingAgreements.Any(a => IsOverlappingTransferAgreement(a, proposal.StartDate.ToDateTimeOffset(), proposal.EndDate?.ToDateTimeOffset()));
    }

    public async Task<List<TransferAgreementProposal>> GetTransferAgreementProposals(Guid organizationId)
    {
        var orgId = OrganizationId.Create(organizationId);
        return await context.TransferAgreementProposals
            .Where(p => p.SenderCompanyId == orgId)
            .ToListAsync();
    }
}
