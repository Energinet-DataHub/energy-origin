using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Proxy.IntegrationTests.Swagger;

public class SwaggerWebApplicationFactory : WebApplicationFactory<Program>
{

    public async Task WithApiVersionDescriptionProvider(Func<IApiVersionDescriptionProvider, Task> withAction)
    {
        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IApiVersionDescriptionProvider>();
        await withAction(provider);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(SwaggerTestAuthHandler.AuthScheme)
                .AddScheme<AuthenticationSchemeOptions, SwaggerTestAuthHandler>(
                    SwaggerTestAuthHandler.AuthScheme, options => { });
        });
    }


    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        RemoveB2CAuthenticationSchemes(host);
        return host;
    }

    private static void RemoveB2CAuthenticationSchemes(IHost host)
    {
        var authenticationSchemeProvider = host.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        authenticationSchemeProvider.RemoveScheme(EnergyOrigin.TokenValidation.b2c.AuthenticationScheme
            .B2CAuthenticationScheme);
        authenticationSchemeProvider.RemoveScheme(EnergyOrigin.TokenValidation.b2c.AuthenticationScheme
            .B2CClientCredentialsCustomPolicyAuthenticationScheme);
        authenticationSchemeProvider.RemoveScheme(EnergyOrigin.TokenValidation.b2c.AuthenticationScheme
            .B2CMitIDCustomPolicyAuthenticationScheme);
    }
}
