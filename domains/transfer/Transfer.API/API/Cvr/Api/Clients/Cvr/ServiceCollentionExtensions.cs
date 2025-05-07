using System;
using IdentityModel.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace API.Cvr.Api.Clients.Cvr;

public static class Startup
{
    public static void AddCvr(this IServiceCollection services)
    {
        services.AddOptions<CvrOptions>().BindConfiguration(CvrOptions.Prefix).ValidateDataAnnotations().ValidateOnStart();
        services.AddHttpClient<ICvrClient, CvrClient>((sp, c) =>
        {
            var cvrOptions = sp.GetRequiredService<IOptions<CvrOptions>>().Value;
            c.BaseAddress = new Uri(cvrOptions.BaseUrl);
            c.SetBasicAuthentication(cvrOptions.User, cvrOptions.Password);
        }).AddTransientHttpErrorPolicy(b => b.WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5)
        }));
    }
}
