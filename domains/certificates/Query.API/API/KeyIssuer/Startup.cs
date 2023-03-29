using API.KeyIssuer.Repositories;
using Marten;
using Marten.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace API.KeyIssuer;

public static class Startup
{
    public static void AddKeyIssuerService(this IServiceCollection services)
    {
        services.ConfigureMarten(o =>
        {
            o.Schema.For<KeyIssuingDocument>().Identity(x => x.MeteringPointOwner);
        });

        services.AddScoped<IKeyIssuingRepository, KeyIssuingRepository>();
        services.AddScoped<IKeyIssuer, KeyHandler>();
    }
}
