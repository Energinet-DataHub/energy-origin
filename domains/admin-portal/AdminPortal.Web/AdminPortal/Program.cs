using System;
using System.Net.Http;
using System.Net.Http.Headers;
using AdminPortal.Options;
using AdminPortal.Services;
using AdminPortal.Utilities;
using AdminPortal.Utilities.Local;
using EnergyOrigin.Setup;
using EnergyOrigin.Setup.Health;
using EnergyOrigin.Setup.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<AdminPortalOptions>().BindConfiguration(AdminPortalOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ClientUriOptions>().BindConfiguration(ClientUriOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<OidcOptions>().BindConfiguration(OidcOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<OtlpOptions>().BindConfiguration(OtlpOptions.Prefix).ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDefaultHealthChecks();

var otlpConfiguration = builder.Configuration.GetSection(OtlpOptions.Prefix);
var otlpOptions = otlpConfiguration.Get<OtlpOptions>()!;

builder.Services.AddOpenTelemetryMetricsAndTracing("AdminPortal.Web", otlpOptions.ReceiverEndpoint);

builder.AddSerilogWithoutOutboxLogs();

builder.Services.AddRazorPages();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddOidcMiddlewareForAdminPortal(builder.Configuration, builder.Environment);

builder.Services.AddHttpClient("Msal")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
    .ConfigureHttpClient(client =>
    {
        client.MaxResponseContentBufferSize = 1024 * 1024;
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    });

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<DelegatingHandler, FakeClientCredentialsTokenHandler>();
}
else
{
    builder.Services.AddSingleton<MsalHttpClientFactoryAdapter>();

    builder.Services.AddTransient(sp =>
    {
        var options = sp.GetRequiredService<IOptions<AdminPortalOptions>>().Value;
        return new ClientCredentialsTokenHandler(
            options.ClientId,
            options.ClientSecret,
            options.TenantId,
            new[] { options.Scope },
            sp.GetRequiredService<MsalHttpClientFactoryAdapter>()
        );
    });
}

builder.Services.AddUpstreamHttpClientsAndServices(builder.Environment);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

var app = builder.Build();

app.MapHealthChecks("/health").AllowAnonymous();

app.UseForwardedHeaders();

app.MapRazorPages();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/ett-admin-portal/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
}
else
{
    app.UseStaticFiles(new StaticFileOptions
    {
        RequestPath = "/ett-admin-portal"
    });
    app.UsePathBase("/ett-admin-portal");
}

app.UseRouting();
app.UseOidcMiddlewareForAdminPortal(app.Environment);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ActiveContracts}/{action=Index}/{id?}");

app.Run();

public partial class Program
{
}
