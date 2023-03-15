using API.Query.API.ApiModels.Requests;
using API.Query.API.Projections;
using FluentValidation;
using Marten;
using Marten.Events.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace API.Query.API;

public static class Startup
{
    public static void AddQueryApi(this IServiceCollection services)
    {
        services.ConfigureMarten(o =>
        {
            o.Projections.Add(new CertificatesByOwnerProjection { EnableDocumentTrackingDuringRebuilds = true });
        });

        services.AddHttpContextAccessor();

        services.AddValidatorsFromAssemblyContaining<CreateContractValidator>();
    }
}
