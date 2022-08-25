using API.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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


    public T? Decrypt<T>(string encryptedState)
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
                        return JsonSerializer.Deserialize<T>(
                            streamReader.ReadToEnd(),
                            new JsonSerializerOptions()
                            {
                                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                            }
                        );
                    }
                }
            }
        }
    }
}
