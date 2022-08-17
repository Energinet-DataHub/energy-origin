using API.Models;
using API.Helpers;
using System.Security.Cryptography;
using System.Text;

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
            byte[] data;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(aes.IV);

                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                    {
                        streamWriter.Write(state.ToString());
                    }
                    data = memoryStream.ToArray();
                }
            }
            return Convert.ToBase64String(data);
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
}
