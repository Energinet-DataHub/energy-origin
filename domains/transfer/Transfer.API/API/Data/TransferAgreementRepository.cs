using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class TransferAgreementRepository : ITransferAgreementRepository
{
    private readonly ApplicationDbContext context;
    public TransferAgreementRepository(ApplicationDbContext context) => this.context = context;

    public async Task<TransferAgreement> AddTransferAgreementToDb(TransferAgreement transferAgreement)
    {
        var agreements = await context.TransferAgreements.Where(t =>
            t.SenderId == transferAgreement.SenderId)
            .ToListAsync();
        var transferAgreementNumber = agreements.Any() ? agreements.Max(ta => ta.TransferAgreementNumber) + 1 : 0;
        transferAgreement.TransferAgreementNumber = transferAgreementNumber;
        context.TransferAgreements.Add(transferAgreement);

        await context.SaveChangesAsync();
        return transferAgreement;
    }

    public async Task<List<TransferAgreement>> GetTransferAgreementsList(Guid subjectId, string receiverTin) =>
        await context.TransferAgreements
            .Where(ta => ta.SenderId == subjectId
                         || ta.ReceiverTin == receiverTin)
            .ToListAsync();

    public Task<List<TransferAgreement>> GetAllTransferAgreements() => context.TransferAgreements.ToListAsync();

    public async Task<TransferAgreement?> GetTransferAgreement(Guid id, string subject, string tin) =>
        await context
            .TransferAgreements
            .Where(agreement => agreement.Id == id && (agreement.SenderId == Guid.Parse(subject) || agreement.ReceiverTin.Equals(tin)))
            .FirstOrDefaultAsync();

    public async Task Save() => await context.SaveChangesAsync();

    public async Task<bool> HasDateOverlap(TransferAgreement transferAgreement)
    {
        var overlappingAgreements = await context.TransferAgreements
            .Where(t => t.SenderId == transferAgreement.SenderId &&
                        t.ReceiverTin == transferAgreement.ReceiverTin &&
                        t.Id != transferAgreement.Id)
            .ToListAsync();

        return overlappingAgreements.Any(a =>
            IsOverlappingTransferAgreement(a, transferAgreement.StartDate, transferAgreement.EndDate)
        );
    }

    private static bool IsOverlappingTransferAgreement(TransferAgreement transferAgreement, DateTimeOffset startDate, DateTimeOffset? endDate) =>
        !(startDate >= transferAgreement.EndDate || endDate <= transferAgreement.StartDate);
}
