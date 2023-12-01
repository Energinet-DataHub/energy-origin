using System.Threading.Tasks;
using Contracts.Certificates.CertificateRejectedInRegistry.V1;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer;

public class CertificateRejectedInRegistryEventHandler : IConsumer<CertificateRejectedInRegistryEvent>
{
    private readonly ICertificateRepository repository;
    private readonly ILogger<CertificateRejectedInRegistryEventHandler> logger;

    public CertificateRejectedInRegistryEventHandler(ICertificateRepository repository, ILogger<CertificateRejectedInRegistryEventHandler> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<CertificateRejectedInRegistryEvent> context)
    {
        var msg = context.Message;

        Certificate? certificate;

        if (msg.MeteringPointType == MeteringPointType.Production)
            certificate = await repository.GetProductionCertificate(msg.CertificateId);
        else if (msg.MeteringPointType == MeteringPointType.Consumption)
            certificate = await repository.GetConsumptionCertificate(msg.CertificateId);
        else
            throw new CertificateDomainException(msg.CertificateId, string.Format("Unsupported meteringPointType: {0}", msg.MeteringPointType));

        if (certificate == null)
        {
            logger.LogError("Certificate with id {msg.CertificateId} could not be found. The certificate was not persisted before being sent to Project Origin.", msg.CertificateId);
            return;
        }

        certificate.Reject(msg.Reason);

        if (msg.MeteringPointType == MeteringPointType.Production)
            await repository.Save((ProductionCertificate)certificate);
        else
            await repository.Save((ConsumptionCertificate)certificate);
    }
}
