using API.Configurations;
using API.ContractService.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using EnergyOrigin.WalletClient;
using WalletClient;

namespace API.ContractService;

public static class Startup
{
    public static void AddContractService(this IServiceCollection services)
    {
        services.AddMeasurementsOptions();
        services.AddScoped<IContractService, ContractServiceImpl>();
        services.AddScoped<IAdminPortalContractService, AdminPortalContractServiceImpl>();

        services.AddScoped<IMeteringPointsClient, MeteringPointsClient>();

        services.AddHttpClient<IMeteringPointsClient, MeteringPointsClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<MeasurementsOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
        });

        services.AddWalletOptions();

        services.AddHttpClient<IWalletClient, EnergyOrigin.WalletClient.WalletClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<WalletOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
        });

        services.AddHttpClient<IStampClient, StampClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<StampOptions>>().Value;
            client.BaseAddress = new Uri(options.Url);
        });
    }
}

public static partial class OptionsExtensions
{
    public static void AddWalletOptions(this IServiceCollection services) =>
        services.AddOptions<WalletOptions>()
            .BindConfiguration(WalletOptions.Wallet)
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
