using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.DemoWorkflow;

public class RegistryConnectorDemoConsumer : IConsumer<SaveDemoInRegistry>
{
    private readonly ILogger<RegistryConnectorDemoConsumer> logger;

    public RegistryConnectorDemoConsumer(ILogger<RegistryConnectorDemoConsumer> logger) => this.logger = logger;

    public async Task Consume(ConsumeContext<SaveDemoInRegistry> context)
    {
        logger.LogInformation("Received {message}", context.Message);

        var sleepTime = RandomNumberGenerator.GetInt32(1, 5);
        await Task.Delay(TimeSpan.FromSeconds(sleepTime));

        var message = new DemoInRegistrySaved
        {
            CorrelationId = context.Message.CorrelationId,
            Foo = context.Message.Foo,
            R = 42
        };

        await context.Publish(message, context.CancellationToken);
    }
}
