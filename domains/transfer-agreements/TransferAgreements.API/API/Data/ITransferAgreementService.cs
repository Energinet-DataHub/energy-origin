using System.Threading.Tasks;

namespace API.Data;

public interface ITransferAgreementService
{
    Task<TransferAgreement> CreateTransferAgreement(TransferAgreement transferAgreement);
}
