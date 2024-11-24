using Testcontainers.RabbitMq;

namespace API.IntegrationTests.Setup;

public class SharedRabbitMqContainer : IAsyncLifetime
{
    private static readonly Lazy<SharedRabbitMqContainer> _instance = new Lazy<SharedRabbitMqContainer>(() => new SharedRabbitMqContainer());
    public static SharedRabbitMqContainer Instance => _instance.Value;

    public RabbitMqContainer Container { get; private set; }

    private SharedRabbitMqContainer()
    {
        Container = new RabbitMqBuilder()
            .WithUsername("guest")
            .WithPassword("guest")
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }
}
