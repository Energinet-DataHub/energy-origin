using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Transfer.Api.Models;

namespace API.Transfer.Api.Repository;

public interface ITransferAgreementHistoryEntryRepository
{
    Task<List<TransferAgreementHistoryEntry>> GetHistoryEntriesForTransferAgreement(Guid transferAgreementId, string subject, string tin);
}
