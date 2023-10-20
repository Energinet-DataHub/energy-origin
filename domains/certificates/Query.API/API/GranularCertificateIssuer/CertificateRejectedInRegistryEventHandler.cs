using System.Threading.Tasks;
using API.Data;
using CertificateValueObjects;
using Contracts.Certificates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer;

public class CertificateRejectedInRegistryEventHandler : IConsumer<CertificateRejectedInRegistryEvent>, IConsumer<Contracts.Certificates.CertificateRejectedInRegistry.V1.CertificateRejectedInRegistryEvent>
{
    private readonly ICertificateRepository repository;
    private readonly ILogger<CertificateRejectedInRegistryEventHandler> logger;

    public CertificateRejectedInRegistryEventHandler(ICertificateRepository repository, ILogger<CertificateRejectedInRegistryEventHandler> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }

    //TODO this function will be deleted after PR merge
    public async Task Consume(ConsumeContext<CertificateRejectedInRegistryEvent> context)
    {
        var msg = context.Message;

        var certificate = await repository.GetProductionCertificate(msg.CertificateId);

        if (certificate == null)
        {
            logger.LogError("Certificate with id {msg.CertificateId} could not be found. The certificate was not persisted before being sent to Project Origin.", msg.CertificateId);
            return;
        }

        certificate.Reject(msg.Reason);

        await repository.Save(certificate);
    }

    public async Task Consume(ConsumeContext<Contracts.Certificates.CertificateRejectedInRegistry.V1.CertificateRejectedInRegistryEvent> context)
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
