using API.Services;

namespace API.Helpers;

public static class ServicesConfiguration
{
    public static void AddCustomServices(this IServiceCollection services)
    {
        services.AddHttpClient<IDataSyncService, DataSyncService>(client =>
        {
            client.BaseAddress = new Uri(Configuration.GetDataSyncEndpoint());
        });
        services.AddTransient<IMeasurementsService, MeasurementsService>();
        services.AddTransient<IConsumptionsCalculator, ConsumptionsCalculator>();
    }
}
