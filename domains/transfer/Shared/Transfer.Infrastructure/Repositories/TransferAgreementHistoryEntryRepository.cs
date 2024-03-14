using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Transfer.Domain.Entities;
using Transfer.Application.Repositories;

namespace DataContext.Repositories;

public class TransferAgreementHistoryEntryRepository : ITransferAgreementHistoryEntryRepository
{
    private readonly ApplicationDbContext context;
    public TransferAgreementHistoryEntryRepository(ApplicationDbContext context) => this.context = context;

    public async Task<List<TransferAgreementHistoryEntry>> GetHistoryEntriesForTransferAgreement(
        Guid transferAgreementId, string subject, string tin) =>
        await context.TransferAgreementHistoryEntries
            .Where(agreement => agreement.TransferAgreementId == transferAgreementId &&
                                (agreement.SenderId == Guid.Parse(subject) || agreement.ReceiverTin.Equals(tin)))
            .ToListAsync();

    public async Task<TransferAgreementHistoryView> GetHistoryEntriesForTransferAgreement(
        Guid transferAgreementId, string subject, string tin, Pagination pagination)
    {
        List<TransferAgreementHistoryEntry> history;
        if (pagination.offset == 0 && pagination.limit == 0)
        {
            history = await context.TransferAgreementHistoryEntries
                .Where(agreement => agreement.TransferAgreementId == transferAgreementId &&
                                    (agreement.SenderId == Guid.Parse(subject) || agreement.ReceiverTin.Equals(tin)))
                .ToListAsync();
        }
        else
        {
            history = await context.TransferAgreementHistoryEntries
                .Where(agreement => agreement.TransferAgreementId == transferAgreementId &&
                                    (agreement.SenderId == Guid.Parse(subject) || agreement.ReceiverTin.Equals(tin)))
                .Skip(pagination.offset)
                .Take(pagination.limit)
                .ToListAsync();
        }


        var totalCount = context.TransferAgreementHistoryEntries
            .Count(agreement => agreement.TransferAgreementId == transferAgreementId &&
                                (agreement.SenderId == Guid.Parse(subject) || agreement.ReceiverTin.Equals(tin)));

        return new TransferAgreementHistoryView(totalCount, history);
    }
}
