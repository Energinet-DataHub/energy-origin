using System.Security.Cryptography;
using IdentityModel;
using Microsoft.IdentityModel.Tokens;

namespace API.Utilities;

public static class JsonWebKeyExtensions
{
    public static SecurityKey ToSecurityKey(this IdentityModel.Jwk.JsonWebKey key) => new RsaSecurityKey(new RSAParameters { Exponent = Base64Url.Decode(key.E), Modulus = Base64Url.Decode(key.N) })
    {
        KeyId = key.Kid
    };
}
