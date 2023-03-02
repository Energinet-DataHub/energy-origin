using System.Threading;
using System.Threading.Tasks;
using API.RabbitMq.Configurations;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace API.IntegrationTest.Infrastructure;

public class RabbitMqContainer : IAsyncLifetime
{
    private readonly RabbitMqTestcontainer testContainer;

    public RabbitMqContainer() =>
        testContainer = new TestcontainersBuilder<RabbitMqTestcontainer>()
            .WithMessageBroker(new RabbitMqTestcontainerConfiguration
            {
                Username = "guest",
                Password = "guest"
            })
            .Build();

    public RabbitMqOptions Options => new()
    {
        Host = testContainer.Hostname,
        Port = testContainer.Port,
        Username = testContainer.Username,
        Password = testContainer.Password
    };
    
    public async Task InitializeAsync() => await testContainer.StartAsync();

    public Task DisposeAsync() => testContainer.StopAsync();
}
