using AdminPortal.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AdminPortal.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly MockHttpMessageHandler _mockHandler;

    public CustomWebApplicationFactory(MockHttpMessageHandler mockHandler)
    {
        _mockHandler = mockHandler;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAggregationService>();

            services.AddScoped<IAggregationService, AggregationService>();

            services.AddHttpClient("FirstPartyApi")
                .ConfigurePrimaryHttpMessageHandler(() => _mockHandler);

            services.AddHttpClient("ContractsApi")
                .ConfigurePrimaryHttpMessageHandler(() => _mockHandler);
        });
    }
}
