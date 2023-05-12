using System.Threading.Tasks;
using Contracts.Certificates;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client;
using ProjectOrigin.Electricity.Client.Models;
using RegistryConnector.Worker.Cache;

namespace RegistryConnector.Worker.Application.EventHandlers
{
    public class ProductionCertificateCreatedEventHandler : IConsumer<ProductionCertificateCreatedEvent>
    {
        private readonly ILogger<ProductionCertificateCreatedEventHandler> logger;
        private readonly ICertificateEventsInMemoryCache cache;
        private readonly RegisterClient registerClient;
        private readonly Key issuerKey;

        public ProductionCertificateCreatedEventHandler(IOptions<RegistryOptions> registryOptions,
            RegisterClient registerClient,
            ILogger<ProductionCertificateCreatedEventHandler> logger,
            ICertificateEventsInMemoryCache cache)
        {
            this.logger = logger;
            this.cache = cache;
            this.registerClient = registerClient;

            issuerKey = Key.Import(SignatureAlgorithm.Ed25519, registryOptions.Value.IssuerPrivateKeyPem, KeyBlobFormat.PkixPrivateKeyText);
        }

        public async Task Consume(ConsumeContext<ProductionCertificateCreatedEvent> context)
        {
            var msg = context.Message;
            var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

            var commandBuilder = new ElectricityCommandBuilder();

            var federatedCertifcateId = new FederatedCertifcateId(
                "RegistryA",
                msg.CertificateId);

            //TODO gsrn parse can be wrong if GSRN starts with 0
            commandBuilder.IssueConsumptionCertificate(
                id: federatedCertifcateId,
                inteval: msg.Period.ToDateInterval(),
                gridArea: msg.GridArea,
                gsrn: ulong.Parse(msg.ShieldedGsrn.Shielded.Value),
                quantity: new ShieldedValue((uint)msg.ShieldedQuantity.Shielded),
                owner: ownerKey.PublicKey,
                issuingBodySigner: issuerKey
            );

            var commandId = await commandBuilder.Execute(registerClient);

            var wrappedMsg = new MessageWrapper<ProductionCertificateCreatedEvent>(context);

            cache.AddCertificateWithCommandId(commandId, wrappedMsg);

            logger.LogInformation("Sent command. Id={id}", HexHelper.ToHex(commandId));
        }

    }
}
