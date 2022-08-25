using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using API.Configuration;
using Microsoft.Extensions.Options;

namespace API.Services;

public class CryptographyService : ICryptographyService
{
    private readonly AuthOptions authOptions;

    public CryptographyService(IOptions<AuthOptions> authOptions)
    {
        this.authOptions = authOptions.Value;
    }

    public string Encrypt<T>(T state)
    {
        using var aes = Aes.Create();

        aes.Key = Encoding.UTF8.GetBytes(authOptions.SecretKey);
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var memoryStream = new MemoryStream();
        memoryStream.Write(aes.IV);

        using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        using var streamWriter = new StreamWriter(cryptoStream);
        var jsonObject = JsonSerializer.Serialize(state);
        streamWriter.Write(jsonObject);

        streamWriter.Close();

        return Convert.ToBase64String(memoryStream.ToArray());
    }

    public T Decrypt<T>(string encryptedState)
    {
        var buffer = Convert.FromBase64String(encryptedState);

        using var memoryStream = new MemoryStream(buffer);

        var iv = new byte[16];
        memoryStream.Read(iv, 0, 16);

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(authOptions.SecretKey);
        aes.IV = iv;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        using var streamReader = new StreamReader(cryptoStream);

        return JsonSerializer.Deserialize<T>(
            streamReader.ReadToEnd(),
            new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }
        );
    }
}
