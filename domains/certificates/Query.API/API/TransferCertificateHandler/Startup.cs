using AggregateRepositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace API.TransferCertificateHandler;

public static class Startup
{
    public static void AddTransferCertificateHandler(this IServiceCollection services)
        => services.TryAddSingleton<IProductionCertificateRepository, ProductionCertificateRepository>();
}
