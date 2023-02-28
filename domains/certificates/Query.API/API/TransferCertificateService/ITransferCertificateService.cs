using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
using Contracts.Transfer;

namespace API.TransferCertificateService;

public interface ITransferCertificateService
{
    Task<TransferProductionCertificateResponse> Get(TransferCertificate transferCertificate);
}
