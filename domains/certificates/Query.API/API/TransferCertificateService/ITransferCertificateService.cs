using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
using CertificateEvents;

namespace API.TransferCertificateService;

public interface ITransferCertificateService
{
    Task<TransferProductionCertificateStatus> Get(TransferCertificate transferCertificate);
}
