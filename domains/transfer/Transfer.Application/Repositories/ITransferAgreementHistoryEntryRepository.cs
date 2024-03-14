using Transfer.Domain.Entities;

namespace Transfer.Application.Repositories;

public interface ITransferAgreementHistoryEntryRepository
{
    Task<List<TransferAgreementHistoryEntry>> GetHistoryEntriesForTransferAgreement(Guid transferAgreementId, string subject, string tin);
    Task<TransferAgreementHistoryView> GetHistoryEntriesForTransferAgreement(Guid transferAgreementId, string subject, string tin, Pagination pagination);
}
