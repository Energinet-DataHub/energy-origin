using System;
using AdminPortal.API;
using AdminPortal.API.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);



    builder.Services.AddControllersWithViews();
    var mockHandler = new MockHttpMessageHandler();

    builder.Services.AddHttpClient("FirstPartyApi", client =>
        {
            client.BaseAddress = new Uri("https://localhost/");
        })
        .ConfigurePrimaryHttpMessageHandler(() => mockHandler);

    builder.Services.AddHttpClient("ContractsApi", client =>
        {
            client.BaseAddress = new Uri("https://localhost/");
        })
        .ConfigurePrimaryHttpMessageHandler(() => mockHandler);

    builder.Services.AddControllersWithViews()
        .AddMicrosoftIdentityUI();
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
            options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Email;
        });

    var requireAuthPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(requireAuthPolicy);

    // builder.Services.AddHttpClient("FirstPartyApi", client =>
    // {
    //     client.BaseAddress = new Uri(builder.Configuration["Apis:FirstParty"] ?? throw new InvalidOperationException());
    // });
    //
    // builder.Services.AddHttpClient("ContractsApi", client =>
    // {
    //     client.BaseAddress = new Uri(builder.Configuration["Apis:Contracts"] ?? throw new InvalidOperationException());
    // });


builder.Services.AddScoped<IAggregationService, AggregationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ActiveContracts}/{action=Index}/{id?}");

app.Run();
