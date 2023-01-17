using System;
using API.CertificateGenerationSignupService.Clients;
using API.CertificateGenerationSignupService.Repositories;
using API.DataSyncSyncer.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.CertificateGenerationSignupService;

public static class Startup
{
    public static void AddCertificateGenerationSignupService(this IServiceCollection services)
    {
        services.AddScoped<ICertificateGenerationSignupService, CertificateGenerationSignupServiceImpl>();

        services.AddScoped<IMeteringPointsClient, MeteringPointsClient>();

        services.AddScoped<IMeteringPointSignupRepository, MeteringPointSignupRepository>(); //TODO: For this to work for DataSyncWorker, it needs to work as a singleton

        services.AddHttpClient<IMeteringPointsClient, MeteringPointsClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DatasyncOptions>>().Value; //TODO: Stealing this from DataSyncSyncer
            client.BaseAddress = new Uri(options.Url);
        });
    }
}
