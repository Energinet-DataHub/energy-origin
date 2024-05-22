using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Proxy.IntegrationTests.Fixtures;

public class IntegrationTestFixture : IAsyncLifetime
{
    private IHost? _ocelotHost;
    public HttpClient? Client { get; private set; }

    public async Task InitializeAsync()
    {
        _ocelotHost = new HostBuilder()
            .ConfigureWebHostDefaults(webBuilder => webBuilder
                .UseTestServer()
                .UseStartup<Startup>()
                .ConfigureAppConfiguration(ConfigureDelegate)).Build();

        await _ocelotHost.StartAsync();
        Client = _ocelotHost.GetTestClient();

        if (Client == null)
            throw new InvalidOperationException("Failed to create HttpClient from test host.");
    }

    private static void ConfigureDelegate(WebHostBuilderContext _, IConfigurationBuilder config)
    {
        config.Sources.Clear();
        config.AddJsonFile("ocelot.test.json");
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_ocelotHost != null)
        {
            await _ocelotHost.StopAsync();
            await _ocelotHost.WaitForShutdownAsync();
            _ocelotHost.Dispose();
        }
    }
}
