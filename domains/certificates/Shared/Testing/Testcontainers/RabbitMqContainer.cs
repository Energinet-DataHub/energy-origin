using System.Text.RegularExpressions;
using EnergyOrigin.Setup.RabbitMq;
using Testcontainers.RabbitMq;
using Xunit;

namespace Testing.Testcontainers;

public partial class RabbitContainer : IAsyncLifetime
{
    private readonly RabbitMqContainer testContainer;

    [GeneratedRegex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:(\d+)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IpAndPortRegex();

    public RabbitContainer() =>
        testContainer = new RabbitMqBuilder()
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

    public RabbitMqOptions Options
    {
        get
        {
            var match = IpAndPortRegex().Match(testContainer.GetConnectionString());

            return new RabbitMqOptions
            {
                Host = testContainer.Hostname,
                Port = int.Parse(match.Groups[1].Value),
                Username = "guest",
                Password = "guest"
            };
        }
    }

    public async Task InitializeAsync() => await testContainer.StartAsync();

    public async Task StopAsync() => await testContainer.StopAsync();

    public Task DisposeAsync() => testContainer.DisposeAsync().AsTask();
}
