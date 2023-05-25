using System.Threading.Tasks;
using Contracts.Certificates;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSec.Cryptography;
using ProjectOrigin.Electricity.Client;
using ProjectOrigin.Electricity.Client.Models;
using RegistryConnector.Worker.Cache;

namespace RegistryConnector.Worker.EventHandlers;

public class ProductionCertificateCreatedEventHandler : IConsumer<ProductionCertificateCreatedEvent>
{
    private readonly ILogger<ProductionCertificateCreatedEventHandler> logger;
    private readonly ICertificateEventsInMemoryCache cache;
    private readonly IssuerKey issuerKey;
    private readonly RegisterClient registerClient;

    public ProductionCertificateCreatedEventHandler(
        RegisterClient registerClient,
        ILogger<ProductionCertificateCreatedEventHandler> logger,
        ICertificateEventsInMemoryCache cache,
        IssuerKey issuerKey)
    {
        this.logger = logger;
        this.cache = cache;
        this.issuerKey = issuerKey;
        this.registerClient = registerClient;
    }

    public async Task Consume(ConsumeContext<ProductionCertificateCreatedEvent> context)
    {
        var msg = context.Message;
        var ownerKey = Key.Create(SignatureAlgorithm.Ed25519);

        var commandBuilder = new ElectricityCommandBuilder();

        var federatedCertificateId = new FederatedCertifcateId(
            "RegistryA",
            msg.CertificateId);

        //TODO GSRN in Project Origin is a ulong. Should be a string? See issue https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1519
        commandBuilder.IssueConsumptionCertificate(
            id: federatedCertificateId,
            inteval: msg.Period.ToDateInterval(),
            gridArea: msg.GridArea,
            gsrn: ulong.Parse(msg.ShieldedGsrn.Shielded.Value),
            quantity: new ShieldedValue((uint)msg.ShieldedQuantity.Shielded),
            owner: ownerKey.PublicKey,
            issuingBodySigner: issuerKey.Value
        );

        var commandId = await commandBuilder.Execute(registerClient);

        var wrappedMsg = new MessageWrapper<ProductionCertificateCreatedEvent>(context);

        cache.AddCertificateWithCommandId(commandId, wrappedMsg);

        logger.LogInformation("Sent command. Id={id}", HexHelper.ToHex(commandId));
    }

}
