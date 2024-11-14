using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Testcontainers.RabbitMq;
using Xunit;

namespace Testing.Testcontainers;

public partial class RabbitMqContainer : IAsyncLifetime
{
    private readonly global::Testcontainers.RabbitMq.RabbitMqContainer testContainer;

    [GeneratedRegex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:(\d+)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static Regex IpAndPortRegex() => new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:(\d+)", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(1000));

    public RabbitMqContainer() =>
        testContainer = new RabbitMqBuilder()
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

    public async Task InitializeAsync() => await testContainer.StartAsync();

    public async Task StopAsync() => await testContainer.StopAsync();

    public Task DisposeAsync() => testContainer.DisposeAsync().AsTask();
}
