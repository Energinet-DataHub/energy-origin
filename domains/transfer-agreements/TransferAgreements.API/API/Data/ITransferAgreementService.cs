using System.Threading.Tasks;
using API.ApiModels.Requests;

namespace API.Data;

public interface ITransferAgreementService
{
    Task<TransferAgreement> CreateTransferAgreement(TransferAgreement transferAgreement);
}
