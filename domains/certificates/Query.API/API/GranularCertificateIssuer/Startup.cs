using API.GranularCertificateIssuer.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace API.GranularCertificateIssuer;

public static class Startup
{
    public static void AddGranularCertificateIssuer(this IServiceCollection services)
        => services.AddSingleton<IProductionCertificateRepository, ProductionCertificateRepository>();
}
