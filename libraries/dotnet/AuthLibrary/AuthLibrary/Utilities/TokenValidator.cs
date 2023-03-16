using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AuthLibrary.Utilities
{
    public class TokenValidator
    {
        public bool TokenValidation(WebApplicationBuilder web, byte[] pem)
        {
            try
            {
                web.Services.AddAuthentication().AddJwtBearer(options =>
                {
                    var rsa = RSA.Create();
                     rsa.ImportFromPem(Encoding.UTF8.GetString(pem));

                    options.MapInboundClaims = false;

                    options.TokenValidationParameters = new()
                    {
                        IssuerSigningKey = new RsaSecurityKey(rsa),
                        ValidAudience = "Users",
                        ValidIssuer = "Us",
                    };
                });
                return true;
            }
            catch (Exception)
            {
                throw;
            }
           
        }
    }

}
