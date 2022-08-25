namespace API.Services;
public interface ICryptographyService
{
    string Encrypt<T>(T state);
    T Decrypt<T>(string encryptedState);
}

