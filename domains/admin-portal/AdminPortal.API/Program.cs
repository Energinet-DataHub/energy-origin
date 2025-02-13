using System;
using AdminPortal.API.Services;
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

// TODO: More intelligent healthcheck, as this application depends on authorization and certificates subsystem
builder.Services.AddHealthChecks();
builder.AddSerilog();

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedPrefix;
    options.ForwardLimit = null;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        var oidcConfig = builder.Configuration.GetSection("OpenIDConnectSettings");

        options.Authority = oidcConfig["Authority"];
        options.ClientId = oidcConfig["ClientId"];
        options.ClientSecret = oidcConfig["ClientSecret"];

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ResponseType = OpenIdConnectResponseType.Code;

        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = false;

        options.MapInboundClaims = false;
        options.CallbackPath = "/signout-callback-oidc";
        options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Email;
    });

var requireAuthPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(requireAuthPolicy);

builder.Services.AddHttpClient("FirstPartyApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Apis:FirstParty"] ?? throw new InvalidOperationException());
});

builder.Services.AddHttpClient("ContractsApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Apis:Contracts"] ?? throw new InvalidOperationException());
});

builder.Services.AddScoped<IAggregationService, AggregationService>();

var app = builder.Build();

app.MapHealthChecks("/health").AllowAnonymous();
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UsePathBase("/ett-admin-portal");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ActiveContracts}/{action=Index}/{id?}");

app.Run();
