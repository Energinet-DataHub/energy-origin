using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EnergyOriginTokenValidation.Utilities;

public static class WebApplicationBuilderExtensions
{
    public static void AddTokenValidation(this WebApplicationBuilder builder, byte[] pem, string? audience = default, string? issuer = default)
    {
        builder.Services.AddAuthentication().AddJwtBearer(options =>
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(Encoding.UTF8.GetString(pem));

            options.MapInboundClaims = false;

            options.TokenValidationParameters = new()
            {
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ValidAudience = audience,
                ValidIssuer = issuer,
                ValidateAudience = audience != null,
                ValidateIssuer = issuer != null,
                ValidateIssuerSigningKey = true
            };
        });
    }
}
