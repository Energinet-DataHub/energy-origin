using Microsoft.Extensions.DependencyInjection;

namespace API.GranularCertificateIssuer;

public static class Startup
{
    public static void AddGranularCertificateIssuer(this IServiceCollection services)
    {
        services.AddHostedService<IssuerWorker>();
        services.AddSingleton<IEnergyMeasuredEventHandler, EnergyMeasuredEventHandler>();
    }
}
