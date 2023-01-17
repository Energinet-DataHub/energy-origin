using System;
using API.CertificateGenerationSignupServiceBla.Clients;
using API.CertificateGenerationSignupServiceBla.Repositories;
using API.DataSyncSyncer.Client.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.CertificateGenerationSignupServiceBla;

public static class Startup
{
    public static void AddCertificateGenerationSignupService(this IServiceCollection services)
    {
        services.AddScoped<ICertificateGenerationSignUpService, CertificateGenerationSignUpServiceImpl>();

        services.AddScoped<IMeteringPointsClient, MeteringPointsClient>();

        services.AddScoped<ICertificateGenerationSignUpRepository, CertificateGenerationSignUpRepository>(); //TODO: For this to work for DataSyncWorker, it needs to work as a singleton

        services.AddHttpClient<IMeteringPointsClient, MeteringPointsClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DatasyncOptions>>().Value; //TODO: Stealing this from DataSyncSyncer
            client.BaseAddress = new Uri(options.Url);
        });
    }
}
