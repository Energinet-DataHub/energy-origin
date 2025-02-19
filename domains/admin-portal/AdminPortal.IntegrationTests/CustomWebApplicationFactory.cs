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
        builder.UseSetting("AUTHORIZATION_CLIENT_ID", "f4abbcfa-9b38-4f05-b36d-6c3186193100")
            .UseSetting("AUTHORIZATION_CLIENT_SECRET", "f4abbcfa-9b38-4f05-b36d-6c3186193100")
            .UseSetting("AUTHORIZATION_TENANT_ID", "f4abbcfa-9b38-4f05-b36d-6c3186193100")
            .UseSetting("CERTIFICATES_CLIENT_ID", "f4abbcfa-9b38-4f05-b36d-6c3186193100")
            .UseSetting("CERTIFICATES_CLIENT_SECRET", "f4abbcfa-9b38-4f05-b36d-6c3186193100")
            .UseSetting("CERTIFICATES_TENANT_ID", "f4abbcfa-9b38-4f05-b36d-6c3186193100");
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
