using System.Threading.Tasks;

namespace API.Data;

public interface ITransferAgreementRepository
{
    Task<TransferAgreement> CreateTransferAgreement(TransferAgreement transferAgreement);
}
