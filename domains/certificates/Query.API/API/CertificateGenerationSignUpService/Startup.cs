using System;
using API.CertificateGenerationSignUpService.Clients;
using API.CertificateGenerationSignUpService.Repositories;
using API.Configurations;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.CertificateGenerationSignUpService;

public static class Startup
{
    public static void AddCertificateGenerationSignUpService(this IServiceCollection services)
    {
        services.ConfigureMarten(o =>
        {
            o.Schema.For<CertificateGenerationSignUp>();
        });

        services.AddScoped<ICertificateGenerationSignUpService, CertificateGenerationSignUpServiceImpl>();

        services.AddScoped<IMeteringPointsClient, MeteringPointsClient>();

        services.AddScoped<ICertificateGenerationSignUpRepository, CertificateGenerationSignUpRepository>();

        services.AddHttpClient<IMeteringPointsClient, MeteringPointsClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DatasyncOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
        });
    }
}
