using API.Configuration;
using API.Features.Emissions;
using API.Features.EnergySources;
using API.Services;
using API.Shared.DataSync;
using API.Shared.EnergiDataService;

namespace API.Extensions;

public static class IServiceCollectionExtension
{
    public static void AddCustomServices(this IServiceCollection services)
    {
        services.AddHttpClient<IEnergiDataService, EnergiDataService>(client => client.BaseAddress = new Uri(Configurations.GetEnergiDataServiceEndpoint()));
        services.AddHttpClient<IDataSyncService, DataSyncService>(client => client.BaseAddress = new Uri(Configurations.GetDataSyncEndpoint()));
        services.AddTransient<IEmissionsService, EmissionsService>();
        services.AddTransient<ISourcesService, SourcesService>();
    }
}
