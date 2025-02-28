using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AdminPortal.Options;
using AdminPortal.Services;
using AdminPortal.Utilities;
using EnergyOrigin.Setup;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using IAuthorizationService = AdminPortal.Services.IAuthorizationService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<OidcOptions>().BindConfiguration(OidcOptions.Prefix)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AdminPortalOptions>().BindConfiguration(AdminPortalOptions.Prefix)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ClientUriOptions>().BindConfiguration(ClientUriOptions.Prefix)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHealthChecks();
builder.AddSerilog();
builder.Services.AddRazorPages();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddScoped<IAggregationQuery, ActiveContractsQuery>();
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
        var oidcOptions = builder.Configuration.GetSection(OidcOptions.Prefix).Get<OidcOptions>()!;

        options.Authority = oidcOptions.Authority;
        options.ClientId = oidcOptions.ClientId;
        options.ClientSecret = oidcOptions.ClientSecret;

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;

        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = false;

        options.MapInboundClaims = false;
        options.CallbackPath = "/signin-oidc";
        options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Email;
        options.Events.OnTokenValidated = context =>
        {
            if (context.Principal == null || !context.Principal.IsInRole("b8fbfff5-243a-4e1d-aab0-6cf8bc2db072"))
            {
                context.Fail("Unauthorized");
            }
            return Task.CompletedTask;
        };
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

builder.Services.AddHttpClient<ICertificatesService, CertificatesService>("CertificatesClient", (sp, client) =>
    {
        var clientUriOptions = sp.GetRequiredService<IOptions<ClientUriOptions>>().Value;
        client.BaseAddress = new Uri(clientUriOptions.Certificates);
    })
    .AddHttpMessageHandler(sp =>
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

builder.Services.AddHttpClient<IAuthorizationService, AuthorizationService>("AuthorizationClient", (sp, client) =>
    {
        var clientUriOptions = sp.GetRequiredService<IOptions<ClientUriOptions>>().Value;
        client.BaseAddress = new Uri(clientUriOptions.Authorization);
    })
    .AddHttpMessageHandler(sp =>
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
