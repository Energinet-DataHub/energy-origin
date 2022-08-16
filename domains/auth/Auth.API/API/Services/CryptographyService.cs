using API.Helpers;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace API.Services;

public class CryptographyService : ICryptographyService
{
    private readonly AuthOptions _authOptions;

    public CryptographyService(IOptions<AuthOptions> authOptions)
    {
        _authOptions = authOptions.Value;
    }

    public string EncryptState(string state)
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
                        streamWriter.Write(state.ToString());
                    }
                }
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
    }

    public string decryptState(string encryptedState)
    {
        byte[] buffer = Convert.FromBase64String(encryptedState);

        using (MemoryStream memoryStream = new MemoryStream(buffer))
        {
            var iv = memoryStream.Read(new byte[16]);
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_authOptions.SecretKey);
                aes.IV = BitConverter.GetBytes(iv);
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
}
