using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EnergyOrigin.TokenValidation.Utilities
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
}
