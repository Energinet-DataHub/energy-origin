using System;
using System.Threading;
using System.Threading.Tasks;
using API.GranularCertificateIssuer;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.IntegrationEventBus;

public static class Startup
{
    public static void AddIntegrationEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(o =>
        {
            o.SetKebabCaseEndpointNameFormatter();

            //o.AddConsumer<EnergyMeasuredConsumer>(cc => cc.UseConcurrentMessageLimit(1));
            o.AddConsumer<TestingMessageHandler>();

            o.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]);
                    h.Password(configuration["RabbitMQ:Password"]);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddHostedService<Producer>();
    }
}

public class TestingMessageHandler : IConsumer<TestingMessage>
{
    private readonly ILogger<TestingMessageHandler> logger;

    public TestingMessageHandler(ILogger<TestingMessageHandler> logger) => this.logger = logger;

    public Task Consume(ConsumeContext<TestingMessage> context)
    {
        logger.LogInformation("Received {message}", context.Message);
        return Task.CompletedTask;
    }
}

public class Producer : BackgroundService
{
    private readonly IBus bus;

    public Producer(IBus bus) => this.bus = bus;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await bus.Publish(new TestingMessage { Foo = "bar" }, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}

public record TestingMessage
{
    public string Foo { get; init; }
}
