using System;
using System.Net.Http.Headers;
using API.DataSyncSyncer.Configurations;
using API.Query.API.Clients;
using API.Query.API.Controllers;
using API.Query.API.Projections;
using FluentValidation;
using Marten;
using Marten.Events.Projections;
using Microsoft.AspNetCore.Http;
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

        services.AddScoped<MeteringPointsClient>();

        services.AddHttpClient<MeteringPointsClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DatasyncOptions>>().Value; //TODO: Stealing this from DataSyncSyncer
            client.BaseAddress = new Uri("http://localhost:8000/");

            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
            var headersAuthorization = httpContextAccessor.HttpContext.Request.Headers.Authorization;

            client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(headersAuthorization);
        });
    }
}
