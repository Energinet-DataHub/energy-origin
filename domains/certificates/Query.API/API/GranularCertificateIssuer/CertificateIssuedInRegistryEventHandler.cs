using System.Threading.Tasks;
using API.Data;
using CertificateValueObjects;
using Contracts.Certificates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer;

public class CertificateIssuedInRegistryEventHandler : IConsumer<CertificateIssuedInRegistryEvent>, IConsumer<Contracts.Certificates.CertificateIssuedInRegistry.V1.CertificateIssuedInRegistryEvent>
{
    private readonly IProductionCertificateRepository productionRepository;
    private readonly IConsumptionCertificateRepository consumptionRepository;
    private readonly ILogger<CertificateIssuedInRegistryEventHandler> logger;

    public CertificateIssuedInRegistryEventHandler(IProductionCertificateRepository productionRepository, IConsumptionCertificateRepository consumptionRepository, ILogger<CertificateIssuedInRegistryEventHandler> logger)
    {
        this.productionRepository = productionRepository;
        this.consumptionRepository = consumptionRepository;
        this.logger = logger;
    }

    //TODO this function will be deleted after PR merge
    public async Task Consume(ConsumeContext<CertificateIssuedInRegistryEvent> context)
    {
        var msg = context.Message;

        var certificate = await productionRepository.Get(msg.CertificateId);

        if (certificate == null)
        {
            logger.LogError("Certificate with id {msg.CertificateId} could not be found. The certificate was not persisted before being sent to Project Origin.", msg.CertificateId);
            return;
        }

        certificate.Issue();

        await productionRepository.Save(certificate);
    }

    public async Task Consume(ConsumeContext<Contracts.Certificates.CertificateIssuedInRegistry.V1.CertificateIssuedInRegistryEvent> context)
    {
        var msg = context.Message;

        Certificate? certificate;

        if(msg.MeteringPointType == MeteringPointType.Production)
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

        certificate.Issue();

        if (msg.MeteringPointType == MeteringPointType.Production)
            await productionRepository.Save((ProductionCertificate)certificate);
        else
            await consumptionRepository.Save((ConsumptionCertificate)certificate);
    }
}
