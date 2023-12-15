using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using Google.Protobuf;
using MassTransit;
using MassTransit.Courier.Contracts;
using MeasurementEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.WalletSystem.V1;
using ProjectOriginClients;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using RegistryConnector.Worker.RoutingSlips;

namespace RegistryConnector.Worker.EventHandlers;

//TODO: Consider different name for class
public class MeasurementEventHandler : IConsumer<EnergyMeasuredIntegrationEvent>
{
    private readonly IEndpointNameFormatter endpointNameFormatter;
    private readonly ProjectOriginOptions projectOriginOptions;
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<MeasurementEventHandler> logger;
    private readonly IKeyGenerator keyGenerator;

    public MeasurementEventHandler(IEndpointNameFormatter endpointNameFormatter, IOptions<ProjectOriginOptions> projectOriginOptions, ApplicationDbContext dbContext, ILogger<MeasurementEventHandler> logger, IKeyGenerator keyGenerator)
    {
        this.endpointNameFormatter = endpointNameFormatter;
        this.projectOriginOptions = projectOriginOptions.Value;
        this.dbContext = dbContext;
        this.logger = logger;
        this.keyGenerator = keyGenerator;
    }

    public async Task Consume(ConsumeContext<EnergyMeasuredIntegrationEvent> context)
    {
        var message = context.Message;

        var contracts = await dbContext.Contracts.Where(c => c.GSRN == message.GSRN).ToListAsync(context.CancellationToken);
        var matchingContract = contracts.FirstOrDefault(c => ShouldEventBeProduced(c, message));
        if (matchingContract == null)
        {
            logger.LogInformation("No certificate created for {Message}", message);
            return;
        }

        var commitment = new SecretCommitmentInfo((uint)message.Quantity);

        var period = new Period(message.DateFrom, message.DateTo);
        var walletDepositEndpointPosition = period.CalculateWalletDepositEndpointPosition();
        if (!walletDepositEndpointPosition.HasValue)
            throw new WalletException($"Cannot determine wallet position for period {period}");

        var builder = new RoutingSlipBuilder(Guid.NewGuid());

        if (matchingContract.MeteringPointType == MeteringPointType.Production)
        {
            var technology = matchingContract.Technology!;

            var productionCertificate = new ProductionCertificate(
                matchingContract.GridArea,
                period,
                technology,
                matchingContract.MeteringPointOwner,
                message.GSRN,
                message.Quantity,
                commitment.BlindingValue.ToArray());

            dbContext.Add(productionCertificate);

            var (ownerPublicKey, issuerKey) = keyGenerator.GenerateKeyInfo(message.Quantity, matchingContract.WalletPublicKey, walletDepositEndpointPosition.Value, matchingContract.GridArea);

            var issuedEvent = Registry.CreateIssuedEventForProduction(
                projectOriginOptions.RegistryName,
                productionCertificate.Id,
                period.ToDateInterval(),
                matchingContract.GridArea,
                message.GSRN,
                technology.TechCode,
                technology.FuelCode,
                commitment,
                ownerPublicKey);

            var transaction = issuedEvent.CreateTransaction(issuerKey);

            BuildRoutingSlip(builder, transaction, commitment, matchingContract, walletDepositEndpointPosition.Value);

            logger.LogInformation("Created production certificate for {Message}", message);
        }
        else if (matchingContract.MeteringPointType == MeteringPointType.Consumption)
        {
            var consumptionCertificate = new ConsumptionCertificate(
                matchingContract.GridArea,
                period,
                matchingContract.MeteringPointOwner,
                message.GSRN,
                message.Quantity,
                commitment.BlindingValue.ToArray());

            dbContext.Add(consumptionCertificate);

            var (ownerPublicKey, issuerKey) = keyGenerator.GenerateKeyInfo(message.Quantity,
                matchingContract.WalletPublicKey, walletDepositEndpointPosition.Value, matchingContract.GridArea);

            var issuedEvent = Registry.CreateIssuedEventForConsumption(
                projectOriginOptions.RegistryName,
                consumptionCertificate.Id,
                period.ToDateInterval(),
                matchingContract.GridArea,
                message.GSRN,
                commitment,
                ownerPublicKey);

            var transaction = issuedEvent.CreateTransaction(issuerKey);

            BuildRoutingSlip(builder, transaction, commitment, matchingContract, walletDepositEndpointPosition.Value);

            logger.LogInformation("Created consumption certificate for {Message}", message);
        }
        else
            throw new CertificateDomainException(string.Format("Unsupported metering point type {0} for message {1}", matchingContract.MeteringPointType, message));

        var routingSlip = builder.Build();

        await context.Execute(routingSlip);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }

    private static bool ShouldEventBeProduced(CertificateIssuingContract contract, EnergyMeasuredIntegrationEvent energyMeasuredIntegrationEvent)
    {
        //TODO: Would be nice to return the reason for not issuing a certificate
        if (!contract.Contains(energyMeasuredIntegrationEvent.DateFrom, energyMeasuredIntegrationEvent.DateTo))
            return false;

        if (energyMeasuredIntegrationEvent.Quantity <= 0)
            return false;

        if (energyMeasuredIntegrationEvent.Quantity > uint.MaxValue) //TODO: Add test
            return false;

        if (energyMeasuredIntegrationEvent.Quality != MeasurementQuality.Measured)
            return false;

        return true;
    }

    private void BuildRoutingSlip(RoutingSlipBuilder builder,
        Transaction transaction,
        SecretCommitmentInfo commitment,
        CertificateIssuingContract matchingContract,
        uint walletDepositEndpointPosition)
    {
        var certificateId = Guid.Parse(transaction.Header.FederatedStreamId.StreamId.Value);

        AddActivity<IssueToRegistryActivity, IssueToRegistryArguments>(builder, new IssueToRegistryArguments(transaction, certificateId));

        AddActivity<WaitForCommittedTransactionActivity, WaitForCommittedTransactionArguments>(builder,
            new WaitForCommittedTransactionArguments(transaction.ToShaId(), certificateId));

        AddActivity<MarkAsIssuedActivity, MarkAsIssuedArguments>(builder,
            new MarkAsIssuedArguments(certificateId, matchingContract.MeteringPointType));

        var receiveRequest = new ReceiveRequest
        {
            CertificateId = transaction.Header.FederatedStreamId,
            Quantity = commitment.Message,
            RandomR = ByteString.CopyFrom(commitment.BlindingValue),
            WalletDepositEndpointPublicKey = ByteString.CopyFrom(matchingContract.WalletPublicKey),
            WalletDepositEndpointPosition = walletDepositEndpointPosition
        };
        AddActivity<SendToWalletActivity, SendToWalletArguments>(builder,
            new SendToWalletArguments(matchingContract.WalletUrl, receiveRequest));

        var issueCertificateFailedConsumerEndpoint = new Uri($"exchange:{endpointNameFormatter.Consumer<IssueCertificateNotCompletedConsumer>()}");
        builder.AddSubscription(issueCertificateFailedConsumerEndpoint, RoutingSlipEvents.Terminated,
            x => x.Send(new IssueCertificateTerminated
            {
                CertificateId = certificateId,
                MeteringPointType = matchingContract.MeteringPointType
            }));
        builder.AddSubscription(issueCertificateFailedConsumerEndpoint, RoutingSlipEvents.Faulted,
            x => x.Send(new IssueCertificateFaulted
            {
                CertificateId = certificateId,
                MeteringPointType = matchingContract.MeteringPointType
            }));
    }

    private void AddActivity<T, TArguments>(RoutingSlipBuilder routingSlipBuilder, TArguments arguments)
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        var uri = new Uri($"exchange:{endpointNameFormatter.ExecuteActivity<T, TArguments>()}");
        routingSlipBuilder.AddActivity(typeof(T).Name, uri, arguments);
    }
}

//TODO: Duplicated

public static class WalletDepositEndpointPositionCalculator
{
    private static readonly long startDate = DateTimeOffset.Parse("2022-01-01T00:00:00Z", CultureInfo.InvariantCulture).ToUnixTimeSeconds();

    public static uint? CalculateWalletDepositEndpointPosition(this Period period)
    {
        var secondsElapsed = period.DateFrom - startDate;

        if (secondsElapsed < 0)
            return null;

        if (secondsElapsed % 60 != 0)
            return null;

        var minutesElapsed = secondsElapsed / 60;
        if (minutesElapsed > int.MaxValue)
            return null;

        return Convert.ToUInt32(minutesElapsed);
    }
}

//TODO: Duplicated

[Serializable]
public class WalletException : Exception
{
    public WalletException(string message) : base(message)
    {
    }
}

public class WorkerBackgroundTester : BackgroundService
{
    private readonly IBus bus;

    public WorkerBackgroundTester(IBus bus)
    {
        this.bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await bus.Publish(new EnergyMeasuredIntegrationEvent("123456", 42, 43, 44, MeasurementQuality.Measured), stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}

public class TransactionConverter : JsonConverter<Transaction>
{
    public override Transaction? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => Transaction.Parser.ParseFrom(reader.GetBytesFromBase64());

    public override void Write(Utf8JsonWriter writer, Transaction value, JsonSerializerOptions options)
        => writer.WriteBase64StringValue(value.ToByteArray());
}

public class ReceiveRequestConverter : JsonConverter<ReceiveRequest>
{
    public override ReceiveRequest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ReceiveRequest.Parser.ParseFrom(reader.GetBytesFromBase64());

    public override void Write(Utf8JsonWriter writer, ReceiveRequest value, JsonSerializerOptions options)
        => writer.WriteBase64StringValue(value.ToByteArray());
}
