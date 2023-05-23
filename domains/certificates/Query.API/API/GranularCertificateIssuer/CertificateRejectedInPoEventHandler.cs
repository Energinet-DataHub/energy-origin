using System.Threading.Tasks;
using AggregateRepositories;
using Contracts.Certificates;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.GranularCertificateIssuer
{
    public class CertificateRejectedInPoEventHandler : IConsumer<CertificateRejectedInPoEvent>
    {
        private readonly IProductionCertificateRepository repository;
        private readonly ILogger<CertificateRejectedInPoEventHandler> logger;

        public CertificateRejectedInPoEventHandler(IProductionCertificateRepository repository, ILogger<CertificateRejectedInPoEventHandler> logger)
        {
            this.repository = repository;
            this.logger = logger;
        }

        public async Task Consume(ConsumeContext<CertificateRejectedInPoEvent> context)
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
}
