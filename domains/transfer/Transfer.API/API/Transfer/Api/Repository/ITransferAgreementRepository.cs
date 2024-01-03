using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataContext.Models;

namespace API.Transfer.Api.Repository;

public interface ITransferAgreementRepository
{
    Task<TransferAgreement> AddTransferAgreementToDb(TransferAgreement transferAgreement);
    Task<TransferAgreement> AddTransferAgreementAndDeleteProposal(TransferAgreement newTransferAgreement, Guid proposalId);
    Task<TransferAgreement?> GetTransferAgreement(Guid id, string subject, string tin);
    Task<List<TransferAgreement>> GetTransferAgreementsList(Guid subjectId, string receiverTin);
    Task<List<TransferAgreement>> GetAllTransferAgreements();
    Task<bool> HasDateOverlap(TransferAgreement transferAgreement);
    Task Save();
    Task<bool> HasDateOverlap(TransferAgreementProposal proposal);
}
