using API.Services;

namespace API.Helpers;

public static class ServicesConfiguration
{
    public static void AddCustomServices(this IServiceCollection services)
    {
        services.AddHttpClient<IEnergiDataService, EnergiDataService>(client =>
        {
            client.BaseAddress = new Uri(Configuration.GetEnergiDataServiceEndpoint());
        });
        services.AddHttpClient<IDataSyncService, DataSyncService>(client =>
        {
            client.BaseAddress = new Uri(Configuration.GetDataSyncEndpoint());
        });
        services.AddTransient<ISourceDeclarationService, SourceDeclarationService>();
    }
}