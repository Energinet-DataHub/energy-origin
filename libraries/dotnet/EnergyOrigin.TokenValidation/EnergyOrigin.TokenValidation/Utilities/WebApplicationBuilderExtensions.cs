using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EnergyOriginTokenValidation.Utilities;

public static class WebApplicationBuilderExtensions
{
    public class ValidationParameters : TokenValidationParameters
    {
        public ValidationParameters(byte[] pem)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(Encoding.UTF8.GetString(pem));
            IssuerSigningKey = new RsaSecurityKey(rsa);
            ValidateIssuerSigningKey = true;
        }
    }

    public static void AddTokenValidation(this WebApplicationBuilder builder, ValidationParameters validationParameters)
    {
        builder.Services.AddAuthentication().AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;

            options.TokenValidationParameters = validationParameters;
        });
    }
}
