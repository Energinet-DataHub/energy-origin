using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EnergyOrigin.TokenValidation.Utilities;

public static class WebApplicationBuilderExtensions
{
    public static void AddTokenValidation(this WebApplicationBuilder builder, ValidationParameters validationParameters) => builder.Services.AddAuthentication().AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = validationParameters;
    });
}
