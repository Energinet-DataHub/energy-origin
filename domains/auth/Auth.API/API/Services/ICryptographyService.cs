namespace API.Services;
public interface ICryptographyService
{
    public string EncryptState(string state);
    public string decryptState(string encryptedState);
}

