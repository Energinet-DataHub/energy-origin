using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Testcontainers.RabbitMq;

namespace Testing.Testcontainers;

public partial class RabbitMqContainer
{
    private readonly global::Testcontainers.RabbitMq.RabbitMqContainer testContainer;

    [GeneratedRegex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:(\d+)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IpAndPortRegex();

    public RabbitMqContainer() =>
        testContainer = new RabbitMqBuilder()
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

    public async Task InitializeAsync() => await testContainer.StartAsync();

    public async Task StopAsync() => await testContainer.StopAsync();

    public Task DisposeAsync() => testContainer.DisposeAsync().AsTask();
}
