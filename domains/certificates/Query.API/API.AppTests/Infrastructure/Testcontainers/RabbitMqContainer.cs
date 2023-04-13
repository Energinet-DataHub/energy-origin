using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using API.RabbitMq.Configurations;
using Testcontainers.RabbitMq;
using Xunit;

namespace API.AppTests.Infrastructure.Testcontainers;

public class RabbitMqContainer : IAsyncLifetime
{
    private readonly global::Testcontainers.RabbitMq.RabbitMqContainer testContainer;

    public RabbitMqContainer() =>
        testContainer = new RabbitMqBuilder()
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

    public RabbitMqOptions Options
    {
        get
        {
            var r = new Regex("\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}:(\\d+)");
            var match = r.Match(testContainer.GetConnectionString());

            return new RabbitMqOptions
            {
                Host = testContainer.Hostname,
                Port = int.Parse(match.Groups[1].Value),
                Username = "guest",
                Password = "guest"
            };
        }
    }

    public async Task InitializeAsync()
    {
        await testContainer.StartAsync();

        var connectionString = testContainer.GetConnectionString();
        Console.WriteLine(connectionString);
    }

    public Task DisposeAsync() => testContainer.DisposeAsync().AsTask();
}
