using System.Threading.Tasks;
using AggregateRepositories;
using Contracts.Certificates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer;

public class CertificateRejectedInRegistryEventHandler : IConsumer<CertificateRejectedInRegistryEvent>
{
    private readonly IProductionCertificateRepository repository;
    private readonly ILogger<CertificateRejectedInRegistryEventHandler> logger;

    public CertificateRejectedInRegistryEventHandler(IProductionCertificateRepository repository, ILogger<CertificateRejectedInRegistryEventHandler> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<CertificateRejectedInRegistryEvent> context)
    {
        var msg = context.Message;

        var certificate = await repository.Get(msg.CertificateId);

        if (certificate == null)
        {
            logger.LogError("Certificate with id {msg.CertificateId} could not be found. The certificate was not persisted before being sent to Project Origin.", msg.CertificateId);
            return;
        }

        certificate.Reject(msg.Reason);

        await repository.Save(certificate);
    }
}
