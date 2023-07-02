using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class TransferAgreementHistoryEntryRepository : ITransferAgreementHistoryEntryRepository
    {
        private readonly ApplicationDbContext context;

        public TransferAgreementHistoryEntryRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<List<TransferAgreementHistoryEntry>> GetHistoryEntriesForTransferAgreement(Guid transferAgreementId, string subject, string tin)
        {
            return await context.TransferAgreementHistoryEntries
                .Where(agreement => agreement.TransferAgreementId == transferAgreementId && (agreement.SenderId == Guid.Parse(subject) || agreement.ReceiverTin.Equals(tin)))
                .ToListAsync();
        }

    }
}
