using EnergyOrigin.TokenValidation.Options;
using Microsoft.AspNetCore.Builder;

namespace EnergyOrigin.TokenValidation.Utilities;

public static class WebApplicationBuilderExtensions
{
    public static void AddTokenValidation(this WebApplicationBuilder builder, TokenValidationOptions validationOptions)
    {
        builder.Services.AddTokenValidation(validationOptions);
    }
}
