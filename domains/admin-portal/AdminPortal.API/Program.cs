using System;
using System.Net.Http;
using System.Net.Http.Headers;
using AdminPortal.API.Services;
using AdminPortal.API.Utilities;
using EnergyOrigin.Setup;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.AddSerilog();
builder.Services.AddRazorPages();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options => options.ExpireTimeSpan = TimeSpan.FromMinutes(30))
    .AddOpenIdConnect(options =>
    {
        var oidcConfig = builder.Configuration.GetSection("OpenIDConnectSettings");

        options.Authority = oidcConfig["Authority"];
        options.ClientId = oidcConfig["ClientId"];
        options.ClientSecret = oidcConfig["ClientSecret"];

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;

        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = false;

        options.MapInboundClaims = false;
        options.CallbackPath = "/signin-oidc";
        options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Email;
    });

builder.Services.AddSingleton<MsalHttpClientFactoryAdapter>();

var requireAuthPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(requireAuthPolicy);

builder.Services.AddHttpClient("Msal")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
    .ConfigureHttpClient(client =>
    {
        client.MaxResponseContentBufferSize = 1024 * 1024;
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    });

builder.Services.AddHttpClient("FirstPartyApi", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Apis:FirstParty"] ?? throw new ArgumentNullException($"Apis:FirstParty configuration missing"));
    })
    .AddHttpMessageHandler(sp => new ClientCredentialsTokenHandler(
        builder.Configuration["AUTHORIZATION_CLIENT_ID"] ?? throw new InvalidOperationException("AUTHORIZATION_CLIENT_ID not set"),
        builder.Configuration["AUTHORIZATION_CLIENT_SECRET"] ?? throw new InvalidOperationException("AUTHORIZATION_CLIENT_SECRET not set"),
        builder.Configuration["AUTHORIZATION_TENANT_ID"] ?? throw new InvalidOperationException("AUTHORIZATION_TENANT_ID not set"),
        new[] { "https://datahubeouenerginet.onmicrosoft.com/energy-origin/.default" },
        sp.GetRequiredService<MsalHttpClientFactoryAdapter>()
        ));

builder.Services.AddHttpClient("ContractsApi", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Apis:Contracts"] ?? throw new ArgumentNullException($"Apis:Contracts configuration missing"));
    })
    .AddHttpMessageHandler(sp => new ClientCredentialsTokenHandler(
        builder.Configuration["CERTIFICATES_CLIENT_ID"] ?? throw new InvalidOperationException("CERTIFICATES_CLIENT_ID not set"),
        builder.Configuration["CERTIFICATES_CLIENT_SECRET"] ?? throw new InvalidOperationException("CERTIFICATES_CLIENT_SECRET not set"),
        builder.Configuration["CERTIFICATES_TENANT_ID"] ?? throw new InvalidOperationException("CERTIFICATES_TENANT_ID not set"),
        new[] { "https://datahubeouenerginet.onmicrosoft.com/energy-origin/.default" },
        sp.GetRequiredService<MsalHttpClientFactoryAdapter>()
    ));

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
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/ett-admin-portal"
});
app.UsePathBase("/ett-admin-portal");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ActiveContracts}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
