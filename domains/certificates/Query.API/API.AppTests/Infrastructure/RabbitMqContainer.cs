using System.Threading.Tasks;
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

    public string Username => testContainer.Username;
    public string Password => testContainer.Password;


    public string Hostname => testContainer.Hostname;
    public int Port => testContainer.Port;

    public async Task InitializeAsync() => await testContainer.StartAsync();

    public Task DisposeAsync() => testContainer.DisposeAsync().AsTask();
}
