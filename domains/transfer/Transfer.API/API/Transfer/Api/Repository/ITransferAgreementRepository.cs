using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataContext.Models;

namespace API.Transfer.Api.Repository;

public interface ITransferAgreementRepository
{
    Task<TransferAgreement> AddTransferAgreementAndDeleteProposal(TransferAgreement newTransferAgreement, Guid proposalId, CancellationToken cancellationToken);
    Task<TransferAgreement> AddTransferAgreement(TransferAgreement newTransferAgreement, CancellationToken cancellationToken);
    Task<TransferAgreement> GetTransferAgreement(Guid id, CancellationToken cancellationToken);
    Task<TransferAgreement?> GetTransferAgreement(Guid id, string subject, string tin, CancellationToken cancellationToken);
    Task<List<TransferAgreement>> GetTransferAgreementsList(Guid organizationId, string receiverTin, CancellationToken cancellationToken);
    Task<List<TransferAgreement>> GetTransferAgreementsList(IList<Guid> organizationIds,
        CancellationToken cancellationToken);
    Task<bool> HasDateOverlap(TransferAgreement transferAgreement, CancellationToken cancellationToken);
    Task<bool> HasDateOverlap(TransferAgreementProposal proposal, CancellationToken cancellationToken);
    Task<List<TransferAgreementProposal>> GetTransferAgreementProposals(Guid organizationId, CancellationToken cancellationToken);
    Task<List<TransferAgreementProposal>> GetTransferAgreementProposals(IList<Guid> organizationIds,
        CancellationToken cancellationToken);
}
