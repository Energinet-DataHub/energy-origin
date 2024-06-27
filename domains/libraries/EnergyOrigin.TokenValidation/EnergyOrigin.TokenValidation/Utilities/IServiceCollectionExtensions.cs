using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.Extensions.DependencyInjection;

namespace EnergyOrigin.TokenValidation.Utilities;

public static class IServiceCollectionExtensions
{
    public static void AddTokenValidation(this IServiceCollection services, TokenValidationOptions validationOptions)
    {
        var validationParameters = new ValidationParameters(validationOptions.PublicKey);
        validationParameters.ValidIssuer = validationOptions.Issuer;
        validationParameters.ValidAudience = validationOptions.Audience;
        services.AddAuthentication().AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = validationParameters;
        });

        services.AddAuthorization(options => options.AddPolicy(PolicyName.RequiresCompany, policy => policy.RequireClaim(UserClaimName.Tin)));
    }
}
