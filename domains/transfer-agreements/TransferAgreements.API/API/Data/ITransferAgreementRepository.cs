using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Models;

namespace API.Data;

public interface ITransferAgreementRepository
{
    Task<TransferAgreement> AddTransferAgreementToDb(TransferAgreement transferAgreement);
    Task<TransferAgreement?> GetTransferAgreement(Guid id, string subject, string tin);
    Task<List<TransferAgreement>> GetTransferAgreementsList(Guid subjectId, string receiverTin);
    Task<List<TransferAgreement>> GetAllTransferAgreements();
    Task<bool> HasDateOverlap(TransferAgreement transferAgreement);
    Task Save();
}
