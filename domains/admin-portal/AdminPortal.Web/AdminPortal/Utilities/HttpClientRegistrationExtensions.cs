using System;
using System.Net.Http;
using AdminPortal.Utilities.Local;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AdminPortal.Utilities;

public static class HttpClientRegistrationExtensions
{
    public static IServiceCollection AddTypedHttpClient<TInterface, TImplementation>(
        this IServiceCollection services,
        Func<IServiceProvider, string> baseAddressFactory,
        Action<HttpClient>? configureClient = null)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddHttpClient<TInterface, TImplementation>((sp, client) =>
            {
                var baseAddress = baseAddressFactory(sp);
                client.BaseAddress = new Uri(baseAddress);

                configureClient?.Invoke(client);
            })
            .AddHttpMessageHandler(sp =>
            {
                var env = sp.GetRequiredService<IWebHostEnvironment>();
                return env.IsDevelopment()
                    ? sp.GetRequiredService<FakeClientCredentialsTokenHandler>()
                    : sp.GetRequiredService<ClientCredentialsTokenHandler>();
            });

        return services;
    }
}


