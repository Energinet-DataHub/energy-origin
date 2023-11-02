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
    private readonly IEndpointNameFormatter endpointNameFormatter;

    public WorkerConsumer(IEndpointNameFormatter endpointNameFormatter)
    {
        this.endpointNameFormatter = endpointNameFormatter;
    }

    public async Task Consume(ConsumeContext<EnergyMeasuredIntegrationEvent> context)
    {
        var someProp = Guid.NewGuid();

        var builder = new RoutingSlipBuilder(Guid.NewGuid());

        AddActivity<IssueToRegistryActivity, IssueToRegistryArguments>(builder, new IssueToRegistryArguments(someProp));
        AddActivity<SendToWalletActivity, SendToWalletArguments>(builder, new SendToWalletArguments(someProp));

        var routingSlip = builder.Build();

        await context.Execute(routingSlip);
    }

    private void AddActivity<T, TArguments>(RoutingSlipBuilder routingSlipBuilder, TArguments arguments)
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        var uri = new Uri($"exchange:{endpointNameFormatter.ExecuteActivity<T, TArguments>()}");
        routingSlipBuilder.AddActivity(typeof(T).Name, uri, arguments);
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

public class SendToWalletActivity : IExecuteActivity<SendToWalletArguments> {
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
