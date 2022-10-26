using Microsoft.Extensions.DependencyInjection;

namespace Issuer.Worker.QueryModelUpdater;

public static class Startup
{
    public static void AddQueryModelUpdater(this IServiceCollection services)
    {
        services.AddHostedService<QueryModelUpdaterWorker>();
    }
}
