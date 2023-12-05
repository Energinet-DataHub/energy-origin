using EnergyOrigin.TokenValidation.Options;
using EnergyOrigin.TokenValidation.Values;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EnergyOrigin.TokenValidation.Utilities;

public static class WebApplicationBuilderExtensions
{
    public static void AddTokenValidation(this WebApplicationBuilder builder, TokenValidationOptions validationOptions)
    {
        var validationParameters = new ValidationParameters(validationOptions.PublicKey);
        validationParameters.ValidIssuer = validationOptions.Issuer;
        validationParameters.ValidAudience = validationOptions.Audience;
        builder.Services.AddAuthentication().AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = validationParameters;
        });
        builder.Services.AddAuthorization(options => options.AddPolicy(PolicyName.RequiresCompany, policy => policy.RequireClaim(UserClaimName.Tin)));
    }
}
