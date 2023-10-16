using System.Threading.Tasks;
using API.Data;
using CertificateValueObjects;
using Contracts.Certificates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer;

public class CertificateRejectedInRegistryEventHandler : IConsumer<CertificateRejectedInRegistryEvent>, IConsumer<Contracts.Certificates.CertificateRejectedInRegistry.V1.CertificateRejectedInRegistryEvent>
{
    private readonly IProductionCertificateRepository productionRepository;
    private readonly IConsumptionCertificateRepository consumptionRepository;
    private readonly ILogger<CertificateRejectedInRegistryEventHandler> logger;

    public CertificateRejectedInRegistryEventHandler(IProductionCertificateRepository productionRepository, IConsumptionCertificateRepository consumptionRepository, ILogger<CertificateRejectedInRegistryEventHandler> logger)
    {
        this.productionRepository = productionRepository;
        this.consumptionRepository = consumptionRepository;
        this.logger = logger;
    }

    //TODO this function will be deleted after PR merge
    public async Task Consume(ConsumeContext<CertificateRejectedInRegistryEvent> context)
    {
        var msg = context.Message;

        var certificate = await productionRepository.Get(msg.CertificateId);

        if (certificate == null)
        {
            logger.LogError("Certificate with id {msg.CertificateId} could not be found. The certificate was not persisted before being sent to Project Origin.", msg.CertificateId);
            return;
        }

        certificate.Reject(msg.Reason);

        await productionRepository.Save(certificate);
    }

    public async Task Consume(ConsumeContext<Contracts.Certificates.CertificateRejectedInRegistry.V1.CertificateRejectedInRegistryEvent> context)
    {
        var msg = context.Message;

        Certificate? certificate;

        if (msg.MeteringPointType == MeteringPointType.Production)
            certificate = await productionRepository.Get(msg.CertificateId);
        else if (msg.MeteringPointType == MeteringPointType.Consumption)
            certificate = await consumptionRepository.Get(msg.CertificateId);
        else
            throw new CertificateDomainException(msg.CertificateId, string.Format("Unsupported meteringPointType: {0}", msg.MeteringPointType));

        if (certificate == null)
        {
            logger.LogError("Certificate with id {msg.CertificateId} could not be found. The certificate was not persisted before being sent to Project Origin.", msg.CertificateId);
            return;
        }

        certificate.Reject(msg.Reason);

        if (msg.MeteringPointType == MeteringPointType.Production)
            await productionRepository.Save((ProductionCertificate)certificate);
        else
            await consumptionRepository.Save((ConsumptionCertificate)certificate);
    }
}
