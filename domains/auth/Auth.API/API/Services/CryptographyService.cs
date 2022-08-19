using API.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace API.Services;
public class CryptographyService : ICryptographyService
{
    string key = Configuration.GetSecretKey();

    public string Encrypt(string state)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(key);
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
                aes.Key = Encoding.UTF8.GetBytes(key);
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

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetSecretKey()));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var handler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim("actor", actor),
                new Claim("subject", subject),
            }),
            Expires = DateTime.UtcNow.AddDays(Configuration.GetTokenExpiryTimeInDays()),
            Issuer = "Energy Origin",
            SigningCredentials = credentials,
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    public T DecodeJwt<T>(string jwtToken)
    {
        var handler = new JwtSecurityTokenHandler();

        var decodedJwt = handler.ReadJwtToken(jwtToken).RawData;

        return JsonSerializer.Deserialize<T>(decodedJwt)!;
    }


}
