namespace API.Services;
public interface ICryptographyService
{
    public string Encrypt(string state);
    public T? Decrypt<T>(string encryptedState);

}

