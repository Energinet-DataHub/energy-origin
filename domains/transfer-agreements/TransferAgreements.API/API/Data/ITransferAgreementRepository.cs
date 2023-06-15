using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.ApiModels.Responses;

namespace API.Data;

public interface ITransferAgreementRepository
{
    Task<TransferAgreement> AddTransferAgreementToDb(TransferAgreement transferAgreement);
    Task<List<TransferAgreement>> GetTransferAgreementsList(Guid subjectId, string receiverTin);
    Task<TransferAgreement?> GetTransferAgreement(Guid id);
    Task<bool> HasDateOverlap(Guid id, DateTimeOffset endDate, Guid senderId, string receiverTin);
    Task Save();
}
