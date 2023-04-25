using System;
using System.Threading.Tasks;
using Contracts.Certificates;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client;
using ProjectOrigin.Electricity.Client.Models;

namespace RegistryConnector.Worker.Application.EventHandlers
{
    public class ProductionCertificateCreatedEventHandler : IConsumer<ProductionCertificateCreatedEvent>
    {
        private readonly ILogger<ProductionCertificateCreatedEventHandler> logger;
        private readonly RegisterClient registerClient;
        private readonly Key issuerKey;

        public ProductionCertificateCreatedEventHandler(IOptions<RegistryOptions> registryOptions, RegisterClient registerClient, ILogger<ProductionCertificateCreatedEventHandler> logger)
        {
            this.logger = logger;
            this.registerClient = registerClient;

            issuerKey = Key.Import(SignatureAlgorithm.Ed25519, registryOptions.Value.IssuerPrivateKeyPem, KeyBlobFormat.PkixPrivateKeyText);
        }

        public async Task Consume(ConsumeContext<ProductionCertificateCreatedEvent> context)
        {
            var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

            const long gsrn = 57000001234567;
            var quantity = new ShieldedValue(150);

            var commandBuilder = new ElectricityCommandBuilder();

            var federatedCertifcateId = new FederatedCertifcateId(
                "RegistryA",
                Guid.NewGuid());

            commandBuilder.IssueConsumptionCertificate(
                id: federatedCertifcateId,
                inteval: new DateInterval(
                    new DateTimeOffset(2022, 10, 1, 12, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2022, 10, 1, 13, 0, 0, TimeSpan.Zero)
                ),
                gridArea: "DK1",
                gsrn: gsrn,
                quantity: quantity,
                owner: ownerKey.PublicKey,
                issuingBodySigner: issuerKey
            );

            var commandId = await commandBuilder.Execute(registerClient);

            logger.LogInformation("Sent command. Id={id}", HexHelper.ToHex(commandId));
        }

    }
}
