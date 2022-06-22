using API.Services;

namespace API.Helpers;

public static class ServicesConfiguration
{
    public static void AddCustomServices(this IServiceCollection services)
    {
        services.AddHttpClient<IEnergyDataService, EnergyDataService>(client =>
        {
            client.BaseAddress = new Uri(Configuration.GetEnergiDataServiceEndpoint());
        });
        services.AddHttpClient<IDataSyncService, DataSyncService>(client =>
        {
            client.BaseAddress = new Uri(Configuration.GetDataSyncEndpoint());
        });
        services.AddTransient<IEmissionsService, EmissionsService>();
        services.AddTransient<IEmissionsCalculator, EmissionsCalculator>();
        services.AddTransient<ISourcesCalculator, SourcesCalculator>();
    }
}
