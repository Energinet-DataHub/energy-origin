using System;
using API.DataSyncSyncer.Configurations;
using API.Query.API.ApiModels.Requests;
using API.Query.API.Clients;
using API.Query.API.Projections;
using FluentValidation;
using Marten;
using Marten.Events.Projections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace API.Query.API;

public static class Startup
{
    public static void AddQueryApi(this IServiceCollection services)
    {
        services.ConfigureMarten(o =>
        {
            o.Projections.Add<CertificatesByOwnerProjection>(ProjectionLifecycle.Inline);
        });

        services.AddHttpContextAccessor();

        services.AddValidatorsFromAssemblyContaining<CreateSignupValidator>();

        services.AddScoped<IMeteringPointsClient, MeteringPointsClient>();

        services.AddHttpClient<IMeteringPointsClient, MeteringPointsClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DatasyncOptions>>().Value; //TODO: Stealing this from DataSyncSyncer
            client.BaseAddress = new Uri(options.Url);
        });
    }
}
