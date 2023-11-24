using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Transfer.Api.Models;
using API.Transfer.Api.Repository.Dto;

namespace API.Transfer.Api.Repository;

public interface ITransferAgreementHistoryEntryRepository
{
    Task<List<TransferAgreementHistoryEntry>> GetHistoryEntriesForTransferAgreement(Guid transferAgreementId, string subject, string tin);
    Task<TransferAgreementHistoryResult> GetHistoryEntriesForTransferAgreementPaginated(Guid transferAgreementId, string subject, string tin, Pagination pagination);
}
