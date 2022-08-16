namespace API.Services;
public interface ICryptographyService
{
    public string Encrypt(string state);
    public string Decrypt(string encryptedState);
}

