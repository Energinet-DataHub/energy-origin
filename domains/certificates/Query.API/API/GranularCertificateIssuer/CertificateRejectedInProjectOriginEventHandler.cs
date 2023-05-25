using System.Threading.Tasks;
using AggregateRepositories;
using Contracts.Certificates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer;

public class CertificateRejectedInProjectOriginEventHandler : IConsumer<CertificateRejectedInProjectOriginEvent>
{
    private readonly IProductionCertificateRepository repository;
    private readonly ILogger<CertificateRejectedInProjectOriginEventHandler> logger;

    public CertificateRejectedInProjectOriginEventHandler(IProductionCertificateRepository repository, ILogger<CertificateRejectedInProjectOriginEventHandler> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<CertificateRejectedInProjectOriginEvent> context)
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