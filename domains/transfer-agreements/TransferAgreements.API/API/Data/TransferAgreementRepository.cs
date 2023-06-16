using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class TransferAgreementRepository : ITransferAgreementRepository
{
    private readonly ApplicationDbContext context;

    public TransferAgreementRepository(ApplicationDbContext context) => this.context = context;

    public async Task<TransferAgreement> AddTransferAgreementToDb(TransferAgreement transferAgreement)
    {
        context.TransferAgreements.Add(transferAgreement);
        await context.SaveChangesAsync();
        return transferAgreement;
    }

    public async Task<List<TransferAgreement>> GetTransferAgreementsList(Guid subjectId, string receiverTin)
    {
        return await context.TransferAgreements
            .Where(ta => ta.SenderId == subjectId
                         || ta.ReceiverTin == receiverTin)
            .ToListAsync();
    }


    public async Task<TransferAgreement?> GetTransferAgreement(Guid id, string subject, string tin)
    {
        return await context
            .TransferAgreements
            .Where(agreement => agreement.Id == id && (agreement.SenderId == Guid.Parse(subject) || agreement.ReceiverTin.Equals(tin)))
            .FirstOrDefaultAsync();
    }

    public async Task Save()
    {
        await context.SaveChangesAsync();
    }

    public async Task<TransferAgreement?> GetTransferAgreement(Guid id) =>
        await context.TransferAgreements
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<bool> HasDateOverlap(Guid id, DateTimeOffset endDate, Guid senderId, string receiverTin) =>
        await context.TransferAgreements
            .AnyAsync(t =>
                t.Id != id &&
                t.SenderId == senderId &&
                t.ReceiverTin == receiverTin &&
                (endDate >= t.StartDate && endDate <= t.EndDate));
}
