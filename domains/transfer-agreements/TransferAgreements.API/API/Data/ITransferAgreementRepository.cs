using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Data;

public interface ITransferAgreementRepository
{
    Task<TransferAgreement> AddTransferAgreementToDb(TransferAgreement transferAgreement);
    Task<TransferAgreement?> GetTransferAgreement(Guid id, string subject, string tin);
    Task<List<TransferAgreement>> GetTransferAgreementsList(Guid subjectId, string receiverTin);
    Task<bool> HasDateOverlap(TransferAgreement transferAgreement);
    Task<List<DateRange>> GetAvailableDateRanges(Guid senderId, string receiverTin);
    Task Save();
}
