using System;
using System.Security.Claims;
using AdminPortal.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AdminPortal.Utilities;

public static class OidcConfigurationExtensions
{
public static IServiceCollection AddOidcMiddlewareForAdminPortal(
        this IServiceCollection services,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(options => options.ExpireTimeSpan = TimeSpan.FromMinutes(30))
        .AddOpenIdConnect(options =>
        {
            var oidc = config.GetSection(OidcOptions.Prefix).Get<OidcOptions>()!;

            options.Authority = oidc.Authority;
            options.ClientId = oidc.ClientId;
            options.ClientSecret = oidc.ClientSecret;

            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = false;
            options.GetClaimsFromUserInfoEndpoint = false;
            options.MapInboundClaims = false;
            options.CallbackPath = "/signin-oidc";
            options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Email;
        });

        var policy = env.IsDevelopment()
            ? new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build()
            : new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        services.AddAuthorizationBuilder().SetFallbackPolicy(policy);

        return services;
    }

    public static IApplicationBuilder UseOidcMiddlewareForAdminPortal(
        this IApplicationBuilder app,
        IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseAuthentication();

            app.Use(async (context, next) =>
            {
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    var identity = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "dev-user"),
                        new Claim(ClaimTypes.Email, "dev@example.com"),
                        new Claim(ClaimTypes.Name, "Developer")
                    }, CookieAuthenticationDefaults.AuthenticationScheme);

                    context.User = new ClaimsPrincipal(identity);
                }

                await next();
            });

            app.UseAuthorization();
        }
        else
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        return app;
    }
}
