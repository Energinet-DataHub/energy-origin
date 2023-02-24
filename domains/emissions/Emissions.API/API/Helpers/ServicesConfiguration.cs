using API.Services;

namespace API.Helpers;

public static class ServicesConfiguration
{
    public static void AddCustomServices(this IServiceCollection services)
    {
        services.AddHttpClient<IEnergiDataService, EnergiDataService>(x => x.BaseAddress = new Uri(Configuration.GetEnergiDataServiceEndpoint()));
        services.AddHttpClient<IDataSyncService, DataSyncService>(x => x.BaseAddress = new Uri(Configuration.GetDataSyncEndpoint()));
        services.AddTransient<IEmissionsService, EmissionsService>();
        services.AddTransient<IEmissionsCalculator, EmissionsCalculator>();
        services.AddTransient<ISourcesCalculator, SourcesCalculator>();
    }
}
