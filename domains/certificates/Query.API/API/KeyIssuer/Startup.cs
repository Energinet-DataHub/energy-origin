using Marten;
using Marten.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace API.KeyIssuer;

public static class Startup
{
    public static void AddKeyIssuerService(this IServiceCollection services) =>
        services.ConfigureMarten(o =>
        {
            o.Schema
                .For<KeyIssuingDocument>()
                .UniqueIndex(UniqueIndexType.Computed, "uidx_meteringpointowner");
        });
}
