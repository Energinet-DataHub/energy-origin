using System.Threading.Tasks;
using EnergyOrigin.Setup.RabbitMq;
using Testcontainers.RabbitMq;
using Xunit;

namespace EnergyTrackAndTrace.Testing.Testcontainers;

public class RabbitMqContainer : IAsyncLifetime
{
    public const string RabbitMqUsername = "guest";
    public const string RabbitMqPassword = "guest";

    private const string RabbitmqImage = "rabbitmq:3.13-management";
    private const int RabbitMqAdminPort = 15672;

    private readonly global::Testcontainers.RabbitMq.RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage(RabbitmqImage)
        .WithUsername(RabbitMqUsername)
        .WithPassword(RabbitMqPassword)
        .WithPortBinding(RabbitMqBuilder.RabbitMqPort, true)
        .WithPortBinding(RabbitMqAdminPort, true)
        .Build();

    public RabbitMqOptions Options => RabbitMqOptions.FromConnectionString(_rabbitMqContainer.GetConnectionString());

    public async Task InitializeAsync()
    {
        await _rabbitMqContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _rabbitMqContainer.DisposeAsync();
    }

    public async Task StopAsync()
    {
        await _rabbitMqContainer.StopAsync();
    }
}
