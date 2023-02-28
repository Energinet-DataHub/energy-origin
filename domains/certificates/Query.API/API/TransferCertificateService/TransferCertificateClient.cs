using System.Threading.Tasks;
using API.Query.API.ApiModels.Requests;
using CertificateEvents;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace API.TransferCertificateService;

public class TransferCertificateClient : ITransferCertificateService
{
    private IRequestClient<TransferCertificate> client;

    public TransferCertificateClient(IRequestClient<TransferCertificate> client)
    {
        this.client = client;
    }

    public async Task<TransferProductionCertificateStatus> Get(TransferCertificate transferCertificate)
    {
        var transferCertificateEvent = new TransferProductionCertificate(
            CurrentOwner: transferCertificate.CurrentOwner,
            NewOwner: transferCertificate.NewOwner,
            CertificateId: transferCertificate.CertificateId
        );

        var response = await client.GetResponse<TransferProductionCertificateStatus>(transferCertificateEvent);

        return response.Message;
    }
}
