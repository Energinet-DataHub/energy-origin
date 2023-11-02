using MassTransit;
using MeasurementEvents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryConnector.Worker.EventHandlers;

public class WorkerConsumer : IConsumer<EnergyMeasuredIntegrationEvent>
{
    public async Task Consume(ConsumeContext<EnergyMeasuredIntegrationEvent> context)
    {
        var someProp = Guid.NewGuid();

        var builder = new RoutingSlipBuilder(Guid.NewGuid());
        builder.AddActivity("IssueToRegistry", new Uri("exchange:issue-to-registry_execute"), new IssueToRegistryArguments(someProp));
        builder.AddActivity("SendToWallet", new Uri("exchange:send-to-wallet_execute"), new SendToWalletArguments(someProp));

        var routingSlip = builder.Build();

        await context.Execute(routingSlip);
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
        logger.LogInformation("Registry. TrackingNumber: {trackingNumber}", context.TrackingNumber);

        return Task.FromResult(context.Completed());
    }
}

public record IssueToRegistryArguments(Guid SomeProp);

public class SendToWalletActivity : IExecuteActivity<SendToWalletArguments> {
    private readonly ILogger<SendToWalletActivity> logger;

    public SendToWalletActivity(ILogger<SendToWalletActivity> logger)
    {
        this.logger = logger;
    }

    public Task<ExecutionResult> Execute(ExecuteContext<SendToWalletArguments> context)
    {
        logger.LogInformation("Wallet. TrackingNumber: {trackingNumber}", context.TrackingNumber);

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
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
