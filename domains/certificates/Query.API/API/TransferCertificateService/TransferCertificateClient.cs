using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
using Contracts.Transfer;
using MassTransit;

namespace API.TransferCertificateService;

public class TransferCertificateClient : ITransferCertificateService
{
    private IRequestClient<TransferCertificate> client;

    public TransferCertificateClient(IRequestClient<TransferCertificate> client)
    {
        this.client = client;
    }

    public async Task<TransferProductionCertificateResponse> Get(TransferCertificate transferCertificate)
    {
        var transferCertificateEvent = new TransferProductionCertificateRequest(
            CurrentOwner: transferCertificate.CurrentOwner,
            NewOwner: transferCertificate.NewOwner,
            CertificateId: transferCertificate.CertificateId
        );

        var response = await client.GetResponse<TransferProductionCertificateResponse>(transferCertificateEvent);

        return response.Message;
    }
}
