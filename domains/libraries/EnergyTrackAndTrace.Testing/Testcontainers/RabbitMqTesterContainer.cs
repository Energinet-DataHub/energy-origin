using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using EnergyOrigin.Setup.RabbitMq;
using Testcontainers.RabbitMq;
using Xunit;

namespace EnergyTrackAndTrace.Testing.Testcontainers;

public class RabbitMqTesterContainer : IAsyncLifetime
{
    public const string RabbitMqUsername = "guest";
    public const string RabbitMqPassword = "guest";

    private const string RabbitmqImage = "rabbitmq:3.13-alpine";
    private const int RabbitMqPort = 5672;

    private readonly RabbitMqContainer _rabbitMqContainer;

    public RabbitMqOptions Options => RabbitMqOptions.FromConnectionString(_rabbitMqContainer.GetConnectionString());

    public RabbitMqTesterContainer()
    {
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage(RabbitmqImage)
            .WithUsername(RabbitMqUsername)
            .WithPassword(RabbitMqPassword)
            .WithPortBinding(RabbitMqPort, true)
            .WithEnvironment("RABBITMQ_FEATURE_FLAGS",
                "quorum_queue,classic_mirrored_queue_version,feature_flags_v2")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(RabbitMqPort))
            .WithCleanUp(true)
            .Build();
    }

    public async ValueTask InitializeAsync() => await _rabbitMqContainer.StartAsync();

    public async ValueTask DisposeAsync() => await _rabbitMqContainer.DisposeAsync();

    public async ValueTask StopAsync() => await _rabbitMqContainer.StopAsync();
}
