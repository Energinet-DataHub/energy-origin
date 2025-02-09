using Testcontainers.RabbitMq;

namespace API.IntegrationTests.Setup.Fixtures;

public class RabbitMqRequired : IAsyncLifetime
{
    public RabbitMqContainer RabbitMqContainer { get; } = new RabbitMqBuilder().WithUsername("guest").WithPassword("guest").Build();

    public async Task InitializeAsync()
    {
        await RabbitMqContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await RabbitMqContainer.DisposeAsync();
    }
}
