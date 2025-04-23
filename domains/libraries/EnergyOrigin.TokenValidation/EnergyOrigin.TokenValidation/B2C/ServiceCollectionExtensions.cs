using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EnergyOrigin.TokenValidation.b2c;

public static class ServiceCollectionExtensions
{
    public static readonly List<string> SubTypeUserClaimValues = [Enum.GetName(SubjectType.User)!, Enum.GetName(SubjectType.User)!.ToLower()];

    public static readonly List<string> BooleanTrueClaimValues = ["true", "True"];

    public static void AddB2C(this IServiceCollection services, B2COptions b2COptions)
    {
        services.AddAuthentication(defaultScheme: AuthenticationScheme.B2CAuthenticationScheme)
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

            var adminPortalPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(AuthenticationScheme.B2CAuthenticationScheme)
                .AddRequirements(new ClaimsAuthorizationRequirement(ClaimType.Sub, new List<string> { b2COptions.AdminPortalEnterpriseAppRegistrationObjectId }))
                .Build();
            options.AddPolicy(Policy.AdminPortal, adminPortalPolicy);
        });

        services.AddScoped<IdentityDescriptor>();
        services.AddScoped<AccessDescriptor>();
        services.AddSingleton<IAuthorizationHandler, TermsAcceptedRequirementHandler>();
    }
}
