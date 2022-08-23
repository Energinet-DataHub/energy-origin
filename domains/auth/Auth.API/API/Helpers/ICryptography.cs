namespace API.Helpers;
public interface ICryptography
{
    public string Encrypt(string state);
    public string Decrypt(string encryptedState);
}

