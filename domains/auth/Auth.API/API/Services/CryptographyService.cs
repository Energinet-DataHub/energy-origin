using API.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using API.Models;

namespace API.Services;

public class CryptographyService : ICryptographyService
{
    private readonly AuthOptions _authOptions;

    public CryptographyService(IOptions<AuthOptions> authOptions)
    {
        _authOptions = authOptions.Value;
    }

    public string Encrypt(string state)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(_authOptions.SecretKey);
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(aes.IV);

                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                    {
                        streamWriter.Write(state);
                    }
                }
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
    }
    public string Decrypt(string encryptedState)
    {
        byte[] buffer = Convert.FromBase64String(encryptedState);
        var iv = new byte[16];
        using (MemoryStream memoryStream = new MemoryStream(buffer))
        {
            memoryStream.Read(iv, 0, 16);
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_authOptions.SecretKey);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader streamReader = new StreamReader(cryptoStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }
    }

    public string EncryptJwt(string actor, string subject)
    {

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authOptions.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var handler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim("actor", actor),
                new Claim("subject", subject),
            }),
            Expires = DateTime.UtcNow.AddDays(int.Parse(_authOptions.TokenExpiryTimeInDays)),
            Issuer = "Energy Origin",
            Audience = "http://energioprindelse.dk",
            SigningCredentials = credentials,
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    public IdTokenInfo DecodeJwtIdToken(string jwtToken)
    {

        var handler = new JwtSecurityTokenHandler();

        var decodedJwt = handler.ReadJwtToken(jwtToken);

        var claims = decodedJwt.Claims;

        // I somehow can't get this to deserialize claims automatically

        throw new NotImplementedException();
        // this doesn't work
        //return JsonSerializer.Deserialize<IdTokenInfo>(decodedJwt.ToString())!;
    }

    public JwtToken DecodeJwtCustom(string jwtToken)
    {
        var handler = new JwtSecurityTokenHandler();

        var decodedJwt = handler.ReadJwtToken(jwtToken);

        var claims = decodedJwt.Claims;

        var actor = claims.First(x => x.Type == "actor");
        var subject = claims.First(x => x.Type == "subject");

        return new JwtToken
        {
            Actor = claims.First(x => x.Type == "actor").Value,
            Subject = claims.First(x => x.Type == "subject").Value
        };
    }

    public bool ValidateJwtToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_authOptions.SecretKey));

        var issuer = "Energy Origin";
        var audience = "http://energioprindelse.dk";

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = key
            }, out SecurityToken validatedToken);
        }
        catch
        {
            return false;
        }
        return true;
    }


}
