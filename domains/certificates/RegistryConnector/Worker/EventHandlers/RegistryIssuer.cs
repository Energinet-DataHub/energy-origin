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
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;
using ProjectOriginClients;

namespace RegistryConnector.Worker.EventHandlers;

public class RegistryIssuer : IConsumer<ProductionCertificateCreatedEvent>, IConsumer<ConsumptionCertificateCreatedEvent>
{
    private readonly ILogger<RegistryIssuer> logger;
    private readonly ProjectOriginOptions projectOriginOptions;

    public RegistryIssuer(
        IOptions<ProjectOriginOptions> projectOriginOptions,
        ILogger<RegistryIssuer> logger)
    {
        this.projectOriginOptions = projectOriginOptions.Value;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductionCertificateCreatedEvent> context)
    {
        var message = context.Message;

        var (commitment, ownerPublicKey, issuerKey) = GenerateKeyInfo(message.Quantity,
            message.BlindingValue, message.WalletPublicKey, message.WalletDepositEndpointPosition, message.GridArea);

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
                    projectOriginOptions.RegistryName,
                    commitment.BlindingValue.ToArray(),
                    commitment.Message,
                    message.WalletPublicKey,
                    message.WalletUrl,
                    message.WalletDepositEndpointPosition));
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

    public async Task Consume(ConsumeContext<ConsumptionCertificateCreatedEvent> context)
    {
        var message = context.Message;

        var (commitment, ownerPublicKey, issuerKey) = GenerateKeyInfo(message.Quantity,
            message.BlindingValue, message.WalletPublicKey, message.WalletDepositEndpointPosition, message.GridArea);

        var issuedEvent = Registry.CreateIssuedEventForConsumption(
            projectOriginOptions.RegistryName,
            message.CertificateId,
            message.Period.ToDateInterval(),
            message.GridArea,
            message.Gsrn.Value,
            commitment,
            ownerPublicKey);

        var request = issuedEvent.CreateSendTransactionRequest(issuerKey);

        using var channel = GrpcChannel.ForAddress(projectOriginOptions.RegistryUrl); //TODO: Is this bad practice? Should the channel be re-used?
        var client = new RegistryService.RegistryServiceClient(channel);

        await client.SendTransactionsAsync(request);

    }

    private (SecretCommitmentInfo, IPublicKey, IPrivateKey) GenerateKeyInfo(long quantity, byte[] blindingValue, byte[] walletPublicKey, uint walletDepositEndpointPosition, string gridArea)
    {
        if (quantity > uint.MaxValue)
            throw new ArgumentOutOfRangeException($"Cannot cast quantity {quantity} to uint");

        var commitment = new SecretCommitmentInfo((uint)quantity, blindingValue);

        var hdPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(walletPublicKey);
        var ownerPublicKey = hdPublicKey.Derive((int)walletDepositEndpointPosition).GetPublicKey();
        var issuerKey = projectOriginOptions.GetIssuerKey(gridArea);

        return (commitment, ownerPublicKey, issuerKey);
    }

    private async Task PollRegistryAndPublishEvent()
}
