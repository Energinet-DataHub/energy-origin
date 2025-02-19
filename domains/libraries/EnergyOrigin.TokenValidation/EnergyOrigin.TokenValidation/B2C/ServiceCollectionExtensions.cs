using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EnergyOrigin.TokenValidation.b2c;

public static class ServiceCollectionExtensions
{
    public static readonly List<string> SubTypeUserClaimValues = [Enum.GetName(SubjectType.User)!, Enum.GetName(SubjectType.User)!.ToLower()];

    public static readonly List<string> BooleanTrueClaimValues = ["true", "True"];

    public static void AddB2C(this IServiceCollection services, B2COptions b2COptions)
    {

        services.AddAuthentication(defaultScheme: AuthenticationScheme.TokenValidation)
            .AddJwtBearer(AuthenticationScheme.B2CAuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.AudienceValidator = (_, _, _) => true;
                options.MetadataAddress = b2COptions.B2CWellKnownUrl;
            })
            .AddJwtBearer(AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.AudienceValidator = (_, _, _) => true;
                options.MetadataAddress = b2COptions.ClientCredentialsCustomPolicyWellKnownUrl;
            })
            .AddJwtBearer(AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidateAudience = false;
                options.TokenValidationParameters.AudienceValidator = (_, _, _) => true;
                options.MetadataAddress = b2COptions.MitIDCustomPolicyWellKnownUrl;
            });

        services.AddAuthorization(options =>
        {
            var frontendOr3rdPartyPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(
                    AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme,
                    AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme)
                .AddRequirements(new TermsAcceptedRequirement())
                .Build();
            options.AddPolicy(Policy.FrontendOr3rdParty, frontendOr3rdPartyPolicy);

            var frontendPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme)
                .RequireClaim(ClaimType.SubType, SubTypeUserClaimValues)
                .RequireClaim(ClaimType.OrgCvr)
                .AddRequirements(new TermsAcceptedRequirement())
                .Build();
            options.AddPolicy(Policy.Frontend, frontendPolicy);

            var frontendWithoutTermsAccepted = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AuthenticationScheme.B2CMitIDCustomPolicyAuthenticationScheme)
                .RequireClaim(ClaimType.SubType, SubTypeUserClaimValues)
                .RequireClaim(ClaimType.OrgCvr)
                .Build();
            options.AddPolicy(Policy.FrontendWithoutTermsAccepted, frontendWithoutTermsAccepted);

            var b2CInternalPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AuthenticationScheme.B2CAuthenticationScheme)
                .AddRequirements(new ClaimsAuthorizationRequirement(ClaimType.Sub, new List<string> { b2COptions.CustomPolicyClientId }))
                .Build();
            options.AddPolicy(Policy.B2CInternal, b2CInternalPolicy);
        });

        services.AddScoped<IdentityDescriptor>();
        services.AddScoped<AccessDescriptor>();
        services.AddSingleton<IAuthorizationHandler, TermsAcceptedRequirementHandler>();
    }

    public static void AddEntra(this IServiceCollection services, EntraOptions entraOptions)
    {
        services.AddAuthentication(defaultScheme: AuthenticationScheme.TokenValidation)
             .AddJwtBearer(AuthenticationScheme.EntraClientCredentials, options =>
             {
                 options.MapInboundClaims = false;
                 options.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuer = true,
                     ValidIssuer = entraOptions.ValidIssuer,
                     ValidateAudience = false,
                     ValidateLifetime = true,
                 };
                 options.MetadataAddress = entraOptions.MetadataAddress;
             });

        services.AddAuthorization(options =>
        {
            var entraInternalPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AuthenticationScheme.EntraClientCredentials)
                .AddRequirements(new ClaimsAuthorizationRequirement(ClaimType.AllowedClientId, new List<string> { entraOptions.AllowedClientId }))
                .Build();

            options.AddPolicy(Policy.EntraInternal, entraInternalPolicy);
        });
    }
}
