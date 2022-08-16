using API.Models;
using API.Helpers;
using System.Security.Cryptography;
using System.Text;

namespace API.Services;
public class CryptographyService : ICryptographyService
{
    string key = Configuration.GetSecretKey();

    public string EncryptState(string state)
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
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader streamReader = new StreamReader(cryptoStream))
                    {
                        var decodedState = streamReader.ReadToEnd();
                        // Removal of unnecessary chars
                        var index = decodedState.IndexOf("Auth");
                        return decodedState[index..decodedState.Length];
                    }
                }
            }
        }
    }
}
