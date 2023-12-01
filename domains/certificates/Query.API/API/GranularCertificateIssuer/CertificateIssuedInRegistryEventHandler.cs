using System.Threading.Tasks;
using Contracts.Certificates.CertificateIssuedInRegistry.V1;
using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer;

public class CertificateIssuedInRegistryEventHandler : IConsumer<CertificateIssuedInRegistryEvent>
{
    private readonly ICertificateRepository repository;
    private readonly ILogger<CertificateIssuedInRegistryEventHandler> logger;

    public CertificateIssuedInRegistryEventHandler(ICertificateRepository repository, ILogger<CertificateIssuedInRegistryEventHandler> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<CertificateIssuedInRegistryEvent> context)
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

        certificate.Issue();

        if (msg.MeteringPointType == MeteringPointType.Production)
            await repository.Save((ProductionCertificate)certificate);
        else
            await repository.Save((ConsumptionCertificate)certificate);
    }
}
