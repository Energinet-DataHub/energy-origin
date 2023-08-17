using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Contracts.Certificates;
using Grpc.Net.Client;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;
using ProjectOriginClients;

namespace RegistryConnector.Worker.EventHandlers;

public class ProductionCertificateCreatedEventHandler : IConsumer<ProductionCertificateCreatedEvent>
{
    private readonly ILogger<ProductionCertificateCreatedEventHandler> logger;
    private readonly ProjectOriginOptions projectOriginOptions;

    public ProductionCertificateCreatedEventHandler(
        IOptions<ProjectOriginOptions> projectOriginOptions,
        ILogger<ProductionCertificateCreatedEventHandler> logger)
    {
        this.projectOriginOptions = projectOriginOptions.Value;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductionCertificateCreatedEvent> context)
    {
        var message = context.Message;

        var commitment = new SecretCommitmentInfo((uint)message.Quantity, message.BlindingValue);

        var hdPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(message.WalletPublicKey);
        var ownerPublicKey = hdPublicKey.Derive(message.WalletPosition).GetPublicKey();

        var issuerKey = projectOriginOptions.GetIssuerKey(message.GridArea);

        var issuedEvent = Registry.CreateIssuedEventForProduction(
            projectOriginOptions.RegistryName,
            message.CertificateId,
            message.Period.ToDateInterval(),
            message.GridArea,
            message.Gsrn.Value,
            message.Technology.TechCode,
            message.Technology.FuelCode,
            commitment,
            ownerPublicKey);

        var request = issuedEvent.CreateSendTransactionRequest(issuerKey);

        using var channel = GrpcChannel.ForAddress(projectOriginOptions.RegistryUrl); //TODO: Is this bad practice? Should the channel be re-used?
        var client = new RegistryService.RegistryServiceClient(channel);

        await client.SendTransactionsAsync(request);

        //TODO: Below polling and waiting is not nice on the message broker. To be fixed in https://github.com/Energinet-DataHub/energy-origin-issues/issues/1639

        var statusRequest = request
            .Transactions
            .Single()
            .CreateStatusRequest();

        logger.LogInformation("Sending status request {statusRequest}", statusRequest);

        var stopWatch = new Stopwatch();
        stopWatch.Start();
        while (true)
        {
            var status = await client.GetTransactionStatusAsync(statusRequest);

            if (status.Status == TransactionState.Committed)
            {
                logger.LogInformation("Certificate {id} issued in registry", message.CertificateId);
                await context.Publish(new CertificateIssuedInRegistryEvent(
                    message.CertificateId,
                    commitment.BlindingValue.ToArray(),
                    commitment.Message,
                    message.WalletPublicKey,
                    message.WalletUrl,
                    message.WalletPosition));
                break;
            }

            if (status.Status == TransactionState.Failed)
            {
                logger.LogInformation("Certificate {id} rejected by registry", message.CertificateId);
                await context.Publish(new CertificateRejectedInRegistryEvent(message.CertificateId, status.Message));
                break;
            }

            await Task.Delay(1000);

            if (stopWatch.Elapsed > TimeSpan.FromMinutes(5))
                throw new TimeoutException($"Timed out waiting for transaction to commit for certificate {message.CertificateId}");
        }
    }
}
