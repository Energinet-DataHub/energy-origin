using AdminPortal.Options;
using AdminPortal.Services;
using AdminPortal.Utilities.Local;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AdminPortal.Utilities;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUpstreamHttpClientsAndServices(this IServiceCollection services, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            services.AddScoped<IAuthorizationService, MockAuthorizationService>();
            services.AddScoped<ICertificatesService, MockCertificatesService>();
            services.AddScoped<IMeasurementsService, MockMeasurementsService>();
        }
        else
        {
            services.AddTypedHttpClient<IAuthorizationService, AuthorizationService>(sp =>
                sp.GetRequiredService<IOptions<ClientUriOptions>>().Value.Authorization);

            services.AddTypedHttpClient<ICertificatesService, CertificatesService>(sp =>
                sp.GetRequiredService<IOptions<ClientUriOptions>>().Value.Certificates);

            services.AddTypedHttpClient<IMeasurementsService, MeasurementsService>(sp =>
                sp.GetRequiredService<IOptions<ClientUriOptions>>().Value.Measurements);
        }

        return services;
    }
}
