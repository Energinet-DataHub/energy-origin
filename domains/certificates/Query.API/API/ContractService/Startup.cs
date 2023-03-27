using System;
using API.Configurations;
using API.ContractService.Clients;
using API.ContractService.Repositories;
using API.KeyIssuer;
using Marten;
using Marten.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.ContractService;

public static class Startup
{
    public static void AddContractService(this IServiceCollection services)
    {
        services.ConfigureMarten(o =>
        {
            o.Schema
                .For<CertificateIssuingContract>()
                .UniqueIndex(UniqueIndexType.Computed, "uidx_gsrn_contractnumber", c => c.GSRN, c => c.ContractNumber);
        });

        services.AddScoped<IContractService, ContractServiceImpl>();

        services.AddScoped<IMeteringPointsClient, MeteringPointsClient>();

        services.AddScoped<ICertificateIssuingContractRepository, CertificateIssuingContractRepository>();

        services.AddScoped<IKeyIssuer, KeyIssuer.KeyIssuer>();

        services.AddHttpClient<IMeteringPointsClient, MeteringPointsClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DatasyncOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
        });
    }
}
