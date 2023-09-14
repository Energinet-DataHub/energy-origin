using API.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace API.GranularCertificateIssuer;

public static class Startup
{
    public static void AddGranularCertificateIssuer(this IServiceCollection services)
        => services.TryAddScoped<IProductionCertificateRepository, ProductionCertificateRepository>();
}
