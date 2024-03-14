using Transfer.Domain.Entities;

namespace Transfer.Application.Repositories;

public interface ITransferAgreementProposalRepository
{
    Task AddTransferAgreementProposal(TransferAgreementProposal proposal);
    Task DeleteTransferAgreementProposal(Guid id);
    Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposal(Guid id);
    Task<TransferAgreementProposal?> GetNonExpiredTransferAgreementProposalAsNoTracking(Guid id);
}

