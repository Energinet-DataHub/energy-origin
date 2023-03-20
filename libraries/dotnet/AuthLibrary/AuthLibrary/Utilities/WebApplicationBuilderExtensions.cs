using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AuthLibrary.Utilities;

public static class WebApplicationBuilderExtensions
{
    public static void AddTokenValidation(this WebApplicationBuilder web, byte[] pem, string audience = "audience", string issuer = "issuer")
    {
        web.Services.AddAuthentication().AddJwtBearer(options =>
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(Encoding.UTF8.GetString(pem));

            options.MapInboundClaims = false;

            options.TokenValidationParameters = new()
            {
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ValidAudience = audience,
                ValidIssuer = issuer,
            };
        });
    }
}
