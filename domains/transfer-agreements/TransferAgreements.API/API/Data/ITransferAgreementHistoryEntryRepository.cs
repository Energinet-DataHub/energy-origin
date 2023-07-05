using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Data;

public interface ITransferAgreementHistoryEntryRepository
{
    Task<List<TransferAgreementHistoryEntry>> GetHistoryEntriesForTransferAgreement(Guid transferAgreementId, string subject, string tin);
}
