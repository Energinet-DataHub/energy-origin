using System;
using System.Threading.Tasks;

namespace API.Data;

public interface ITransferAgreementRepository
{
    Task<TransferAgreement> AddTransferAgreementToDb(TransferAgreement transferAgreement);
    Task<TransferAgreement> GetTransferAgreement(Guid id);
}
