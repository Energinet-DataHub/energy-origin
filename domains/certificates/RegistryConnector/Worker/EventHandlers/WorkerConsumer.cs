using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using Google.Protobuf;
using Grpc.Net.Client;
using MassTransit;
using MeasurementEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Common.V1;
using ProjectOrigin.HierarchicalDeterministicKeys.Implementations;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;
using ProjectOrigin.PedersenCommitment;
using ProjectOrigin.Registry.V1;
using ProjectOrigin.WalletSystem.V1;
using ProjectOriginClients;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryConnector.Worker.EventHandlers;

public class WorkerConsumer : IConsumer<EnergyMeasuredIntegrationEvent>
{
    private readonly IEndpointNameFormatter endpointNameFormatter;
    private readonly ProjectOriginOptions projectOriginOptions;
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<WorkerConsumer> logger;

    public WorkerConsumer(IEndpointNameFormatter endpointNameFormatter, IOptions<ProjectOriginOptions> projectOriginOptions, ApplicationDbContext dbContext, ILogger<WorkerConsumer> logger)
    {
        this.endpointNameFormatter = endpointNameFormatter;
        this.projectOriginOptions = projectOriginOptions.Value;
        this.dbContext = dbContext;
        this.logger = logger;
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
            var technology = new Technology(FuelCode: "F00000000", TechCode: "T070000");
            var productionCertificate = new ProductionCertificate(
                matchingContract.GridArea,
                period,
                technology,
                matchingContract.MeteringPointOwner,
                message.GSRN,
                message.Quantity,
                commitment.BlindingValue.ToArray());

            dbContext.Add(productionCertificate);
            await dbContext.SaveChangesAsync(context.CancellationToken);
            
            var (ownerPublicKey, issuerKey) = GenerateKeyInfo(message.Quantity, matchingContract.WalletPublicKey, walletDepositEndpointPosition.Value, matchingContract.GridArea);

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
            
            AddActivity<IssueToRegistryActivity, IssueToRegistryArguments>(builder, new IssueToRegistryArguments(transaction));
            AddActivity<WaitForCommittedTransactionActivity, WaitForCommittedTransactionArguments>(builder, new WaitForCommittedTransactionArguments(transaction.ToShaId()));
            AddActivity<MarkAsIssuedActivity, MarkAsIssuedArguments>(builder, new MarkAsIssuedArguments(productionCertificate.Id, MeteringPointType.Production));

            var receiveRequest = new ReceiveRequest
            {
                CertificateId = new FederatedStreamId
                {
                    Registry = projectOriginOptions.RegistryName,
                    StreamId = new Uuid { Value = productionCertificate.Id.ToString() }
                },
                Quantity = (uint)message.Quantity,
                RandomR = ByteString.CopyFrom(commitment.BlindingValue),
                WalletDepositEndpointPublicKey = ByteString.CopyFrom(matchingContract.WalletPublicKey),
                WalletDepositEndpointPosition = walletDepositEndpointPosition.Value
            };
            AddActivity<SendToWalletActivity, SendToWalletArguments>(builder, new SendToWalletArguments(matchingContract.WalletUrl, receiveRequest));

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
            await dbContext.SaveChangesAsync(context.CancellationToken);
            //await repository.Save(consumptionCertificate, context.CancellationToken);

            //TODO Save to eventstore and publish event must happen in same transaction. See issue https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1518
            //await context.Publish(new ConsumptionCertificateCreatedEvent(
            //    consumptionCertificate.Id,
            //    matchingContract.GridArea,
            //    period,
            //    matchingContract.MeteringPointOwner,
            //    new Gsrn(message.GSRN),
            //    commitment.BlindingValue.ToArray(),
            //    message.Quantity,
            //    matchingContract.WalletPublicKey,
            //    matchingContract.WalletUrl,
            //    walletDepositEndpointPosition.Value));

            logger.LogInformation("Created consumption certificate for {Message}", message);
        }
        else
            throw new CertificateDomainException(string.Format("Unsupported metering point type {0} for message {1}", matchingContract.MeteringPointType, message));


        
        var routingSlip = builder.Build();

        //TODO Save to eventstore and publish event must happen in same transaction. See issue https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1518
        await context.Execute(routingSlip);
    }

    private static bool ShouldEventBeProduced(CertificateIssuingContract contract, EnergyMeasuredIntegrationEvent energyMeasuredIntegrationEvent)
    {
        if (!contract.Contains(energyMeasuredIntegrationEvent.DateFrom, energyMeasuredIntegrationEvent.DateTo))
            return false;

        if (energyMeasuredIntegrationEvent.Quantity <= 0)
            return false;

        if (energyMeasuredIntegrationEvent.Quality != MeasurementQuality.Measured)
            return false;

        return true;
    }

    private (IPublicKey, IPrivateKey) GenerateKeyInfo(long quantity, byte[] walletPublicKey, uint walletDepositEndpointPosition, string gridArea)
    {
        if (quantity > uint.MaxValue)
            throw new ArgumentOutOfRangeException($"Cannot cast quantity {quantity} to uint");

        var hdPublicKey = new Secp256k1Algorithm().ImportHDPublicKey(walletPublicKey);
        var ownerPublicKey = hdPublicKey.Derive((int)walletDepositEndpointPosition).GetPublicKey();
        var issuerKey = projectOriginOptions.GetIssuerKey(gridArea);

        return (ownerPublicKey, issuerKey);
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

    protected WalletException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

public class IssueToRegistryActivity : IExecuteActivity<IssueToRegistryArguments>
{
    private readonly ProjectOriginOptions projectOriginOptions;
    private readonly ILogger<IssueToRegistryActivity> logger;

    public IssueToRegistryActivity(IOptions<ProjectOriginOptions> projectOriginOptions, ILogger<IssueToRegistryActivity> logger)
    {
        this.projectOriginOptions = projectOriginOptions.Value;
        this.logger = logger;
    }

    public async Task<ExecutionResult> Execute(ExecuteContext<IssueToRegistryArguments> context)
    {
        logger.LogInformation("Registry. TrackingNumber: {trackingNumber}. Arguments: {args}. Retry {r1} {r2} {r3}", context.TrackingNumber, context.Arguments, context.GetRetryAttempt(), context.GetRetryCount(), context.GetRedeliveryCount());

        var request = new SendTransactionsRequest();
        request.Transactions.Add(context.Arguments.Transaction);

        using var channel = GrpcChannel.ForAddress(projectOriginOptions.RegistryUrl); //TODO: Is this bad practice? Should the channel be re-used?
        var client = new RegistryService.RegistryServiceClient(channel);

        await client.SendTransactionsAsync(request);

        return context.Completed();
    }
}

public class IssueToRegistryActivityDefinition : ExecuteActivityDefinition<IssueToRegistryActivity, IssueToRegistryArguments>
{
    protected override void ConfigureExecuteActivity(IReceiveEndpointConfigurator endpointConfigurator,
        IExecuteActivityConfigurator<IssueToRegistryActivity, IssueToRegistryArguments> executeActivityConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(3, TimeSpan.FromMilliseconds(500)));
    }
}

public record IssueToRegistryArguments(Transaction Transaction);

public class WaitForCommittedTransactionActivity : IExecuteActivity<WaitForCommittedTransactionArguments>
{
    private readonly ProjectOriginOptions projectOriginOptions;

    public WaitForCommittedTransactionActivity(IOptions<ProjectOriginOptions> projectOriginOptions)
    {
        this.projectOriginOptions = projectOriginOptions.Value;
    }

    public async Task<ExecutionResult> Execute(ExecuteContext<WaitForCommittedTransactionArguments> context)
    {
        using var channel = GrpcChannel.ForAddress(projectOriginOptions.RegistryUrl); //TODO: Is this bad practice? Should the channel be re-used?
        var client = new RegistryService.RegistryServiceClient(channel);
        var statusRequest = new GetTransactionStatusRequest
        {
            Id = context.Arguments.ShaId
        };
        
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        while (true)
        {
            var status = await client.GetTransactionStatusAsync(statusRequest);

            if (status.Status == TransactionState.Committed)
            {
                return context.Completed();
                //logger.LogInformation("Certificate {id} issued in registry", certificateId);
                //await context.Publish(new CertificateIssuedInRegistryEvent(
                //    certificateId,
                //    projectOriginOptions.RegistryName,
                //    commitment.BlindingValue.ToArray(),
                //    commitment.Message,
                //    meteringPointType,
                //    walletPublicKey,
                //    walletUrl,
                //    walletDepositEndpointPosition));
                break;
            }

            if (status.Status == TransactionState.Failed)
            {
                //TODO:
                return context.Completed();
                //logger.LogInformation("Certificate {id} rejected by registry", certificateId);
                //await context.Publish(new CertificateRejectedInRegistryEvent(certificateId, meteringPointType, status.Message));
                //break;
            }

            await Task.Delay(1000);

            if (stopWatch.Elapsed > TimeSpan.FromMinutes(5))
                throw new TimeoutException($"Timed out waiting for transaction to commit for certificate");
        }
    }
}

public record WaitForCommittedTransactionArguments(string ShaId);

public class MarkAsIssuedActivity : IExecuteActivity<MarkAsIssuedArguments>
{
    private readonly ApplicationDbContext dbContext;

    public MarkAsIssuedActivity(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ExecutionResult> Execute(ExecuteContext<MarkAsIssuedArguments> context)
    {
        Certificate? certificate = context.Arguments.MeteringPointType == MeteringPointType.Production
            ? await dbContext.ProductionCertificates.FindAsync(new object?[] { context.Arguments.CertificateId },
                context.CancellationToken)
            : await dbContext.ConsumptionCertificates.FindAsync(new object?[] { context.Arguments.CertificateId },
                context.CancellationToken);

        certificate.Issue();

        await dbContext.SaveChangesAsync(context.CancellationToken);

        return context.Completed();
    }
}

public record MarkAsIssuedArguments(Guid CertificateId, MeteringPointType MeteringPointType);

public class SendToWalletActivity : IExecuteActivity<SendToWalletArguments>
{
    private readonly ILogger<SendToWalletActivity> logger;

    public SendToWalletActivity(ILogger<SendToWalletActivity> logger)
    {
        this.logger = logger;
    }

    public async Task<ExecutionResult> Execute(ExecuteContext<SendToWalletArguments> context)
    {
        logger.LogInformation("Wallet. TrackingNumber: {trackingNumber}. Arguments: {args}", context.TrackingNumber, context.Arguments);

        // TODO: Think this should be handled earlier - in the routing slip builder
        //if (message.Quantity > uint.MaxValue)
        //    throw new ArgumentOutOfRangeException($"Cannot cast quantity {message.Quantity} to uint");

        using var channel = GrpcChannel.ForAddress(context.Arguments.WalletUrl);
        var client = new ReceiveSliceService.ReceiveSliceServiceClient(channel);

        await client.ReceiveSliceAsync(context.Arguments.ReceiveRequest);

        return context.Completed();
    }
}

public record SendToWalletArguments(string WalletUrl, ReceiveRequest ReceiveRequest);

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
