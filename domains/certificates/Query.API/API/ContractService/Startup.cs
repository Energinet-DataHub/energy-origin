using API.Configurations;
using API.ContractService.Clients;
using API.ContractService.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectOrigin.WalletSystem.V1;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace API.ContractService;

public static class Startup
{
    public static void AddContractService(this IServiceCollection services)
    {
        services.AddScoped<IContractService, ContractServiceImpl>();

        services.AddScoped<IMeteringPointsClient, MeteringPointsClient>();

        services.AddScoped<ICertificateIssuingContractRepository, CertificateIssuingContractRepository>();

        services.AddHttpClient<IMeteringPointsClient, MeteringPointsClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DatasyncOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
        });

        services.AddWalletOptions();

        services.AddGrpcClient<WalletService.WalletServiceClient>((sp, o) =>
            {
                var options = sp.GetRequiredService<IOptions<WalletOptions>>().Value;
                o.Address = new Uri(options.Url);
                o.ChannelOptionsActions.Add(channelOptions => channelOptions.UnsafeUseInsecureChannelCallCredentials = true);
            })
            .AddCallCredentials((context, metadata, sp) =>
            {
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                if (AuthenticationHeaderValue.TryParse(httpContextAccessor.HttpContext?.Request.Headers["Authorization"], out var authentication))
                {
                    metadata.Add("Authorization", $"{authentication.Scheme} {authentication.Parameter}");
                }

                return Task.CompletedTask;
            });
    }
}
