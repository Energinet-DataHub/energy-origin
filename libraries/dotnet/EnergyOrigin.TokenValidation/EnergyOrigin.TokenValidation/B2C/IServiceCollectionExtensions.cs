using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EnergyOrigin.TokenValidation.b2c;

public static class IServiceCollectionExtensions
{
    public static void AddB2CAndTokenValidation(this IServiceCollection services, B2COptions b2COptions, TokenValidationOptions validationOptions)
    {
        var tokenValidationParameters = new ValidationParameters(validationOptions.PublicKey);
        tokenValidationParameters.ValidIssuer = validationOptions.Issuer;
        tokenValidationParameters.ValidAudience = validationOptions.Audience;

        services.AddAuthentication(AuthenticationScheme.TokenValidation)
            .AddJwtBearer(AuthenticationScheme.B2CAuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.Audience = b2COptions.Audience;
                options.MetadataAddress = b2COptions.B2CWellKnownUrl;
            })
            .AddJwtBearer(AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.Audience = b2COptions.Audience;
                options.MetadataAddress = b2COptions.ClientCredentialsCustomPolicyWellKnownUrl;
            })
            .AddJwtBearer(AuthenticationScheme.B2CMitICustomPolicyDAuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.Audience = b2COptions.Audience;
                options.MetadataAddress = b2COptions.MitIDCustomPolicyWellKnownUrl;
            })
            .AddJwtBearer(AuthenticationScheme.TokenValidation, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = tokenValidationParameters;
            });

        services.AddAuthorization(options =>
        {
            var b2CPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(
                    AuthenticationScheme.B2CClientCredentialsCustomPolicyAuthenticationScheme,
                    AuthenticationScheme.B2CMitICustomPolicyDAuthenticationScheme)
                .Build();
            options.AddPolicy(Policy.B2CPolicy, b2CPolicy);

            var b2CCustomPolicyClientPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AuthenticationScheme.B2CAuthenticationScheme)
                .AddRequirements(new ClaimsAuthorizationRequirement(ClaimType.Sub, new List<string> { b2COptions.CustomPolicyClientId }))
                .Build();
            options.AddPolicy(Policy.B2CCustomPolicyClientPolicy, b2CCustomPolicyClientPolicy);

            var tokenValidationRequiredCompanyPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AuthenticationScheme.TokenValidation)
                .RequireClaim(UserClaimName.Tin)
                .Build();
            options.AddPolicy(PolicyName.RequiresCompany, tokenValidationRequiredCompanyPolicy);
        });
    }
}