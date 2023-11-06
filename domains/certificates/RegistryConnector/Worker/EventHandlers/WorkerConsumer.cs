using DataContext;
using DataContext.Models;
using DataContext.ValueObjects;
using MassTransit;
using MeasurementEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectOrigin.PedersenCommitment;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryConnector.Worker.EventHandlers;

public class WorkerConsumer : IConsumer<EnergyMeasuredIntegrationEvent>
{
    private readonly IEndpointNameFormatter endpointNameFormatter;
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<WorkerConsumer> logger;

    public WorkerConsumer(IEndpointNameFormatter endpointNameFormatter, ApplicationDbContext dbContext, ILogger<WorkerConsumer> logger)
    {
        this.endpointNameFormatter = endpointNameFormatter;
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

        if (matchingContract.MeteringPointType == MeteringPointType.Production)
        {
            var productionCertificate = new ProductionCertificate(
                matchingContract.GridArea,
                period,
                new Technology(FuelCode: "F00000000", TechCode: "T070000"),
                matchingContract.MeteringPointOwner,
                message.GSRN,
                message.Quantity,
                commitment.BlindingValue.ToArray());

            dbContext.Add(productionCertificate);
            await dbContext.SaveChangesAsync(context.CancellationToken);
            //await repository.Save(productionCertificate, context.CancellationToken);

            //TODO Save to eventstore and publish event must happen in same transaction. See issue https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1518
            //await context.Publish(new ProductionCertificateCreatedEvent(
            //    productionCertificate.Id,
            //    matchingContract.GridArea,
            //    period,
            //    new Technology(FuelCode: "F00000000", TechCode: "T070000"),
            //    matchingContract.MeteringPointOwner,
            //    new Gsrn(message.GSRN),
            //    commitment.BlindingValue.ToArray(),
            //    message.Quantity,
            //    matchingContract.WalletPublicKey,
            //    matchingContract.WalletUrl,
            //    walletDepositEndpointPosition.Value));

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

        var someProp = Guid.NewGuid();

        var builder = new RoutingSlipBuilder(Guid.NewGuid());

        AddActivity<IssueToRegistryActivity, IssueToRegistryArguments>(builder, new IssueToRegistryArguments(someProp));
        AddActivity<SendToWalletActivity, SendToWalletArguments>(builder, new SendToWalletArguments(someProp));

        var routingSlip = builder.Build();

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
    private readonly ILogger<IssueToRegistryActivity> logger;

    public IssueToRegistryActivity(ILogger<IssueToRegistryActivity> logger)
    {
        this.logger = logger;
    }

    public Task<ExecutionResult> Execute(ExecuteContext<IssueToRegistryArguments> context)
    {
        logger.LogInformation("Registry. TrackingNumber: {trackingNumber}. Arguments: {args}. Retry {r1} {r2} {r3}", context.TrackingNumber, context.Arguments, context.GetRetryAttempt(), context.GetRetryCount(), context.GetRedeliveryCount());

        throw new NotImplementedException("hello");

        //return Task.FromResult(context.Faulted());
        return Task.FromResult(context.Completed());
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

public record IssueToRegistryArguments(Guid SomeProp);

public class SendToWalletActivity : IExecuteActivity<SendToWalletArguments>
{
    private readonly ILogger<SendToWalletActivity> logger;

    public SendToWalletActivity(ILogger<SendToWalletActivity> logger)
    {
        this.logger = logger;
    }

    public Task<ExecutionResult> Execute(ExecuteContext<SendToWalletArguments> context)
    {
        logger.LogInformation("Wallet. TrackingNumber: {trackingNumber}. Arguments: {args}", context.TrackingNumber, context.Arguments);

        return Task.FromResult(context.Completed());
    }
}

public record SendToWalletArguments(Guid SomeProp);

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
