using System;
using API.Configurations;
using API.ContractService.Clients;
using API.ContractService.Repositories;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.ContractService;

public static class Startup
{
    public static void AddContractService(this IServiceCollection services)
    {
        services.ConfigureMarten(o =>
        {
            o.Schema.For<CertificateIssuingContract>();
        });

        services.AddScoped<IContractService, ContractServiceImpl>();

        services.AddScoped<IMeteringPointsClient, MeteringPointsClient>();

        services.AddScoped<ICertificateIssuingContractRepository, CertificateIssuingContractRepository>();

        services.AddHttpClient<IMeteringPointsClient, MeteringPointsClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DatasyncOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
        });
    }
}
