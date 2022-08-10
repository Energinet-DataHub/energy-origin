using API.Models;
using API.Helpers;
using System.Security.Cryptography;
using System.Text;

namespace API.Services;
public class CryptographyService : ICryptographyService
{
    string key = Configuration.GetSecretKey();

    public string EncryptState(AuthState state)
    {
        byte[] iv = new byte[16];
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = iv;
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                    {
                        streamWriter.Write(state.ToString());
                    }
                    byte[] data  = memoryStream.ToArray();
                    return Convert.ToBase64String(data);
                }
            }

        }
    }

    public string decryptState(string encryptedState)
    {
        byte[] iv = new byte[16];
        byte[] buffer = Convert.FromBase64String(encryptedState);

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = iv;
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
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
