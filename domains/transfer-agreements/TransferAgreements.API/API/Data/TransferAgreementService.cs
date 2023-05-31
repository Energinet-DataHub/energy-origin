using System.Threading.Tasks;
using API.ApiModels;

namespace API.Data
{
    public class TransferAgreementService : ITransferAgreementService
    {
        public async Task<TransferAgreement> CreateTransferAgreement(TransferAgreement transferAgreement)
        {
            return transferAgreement;
        }
    }
}
