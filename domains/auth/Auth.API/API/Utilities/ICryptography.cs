namespace API.Utilities;

public interface ICryptography
{
    string Encrypt<T>(T state);
    T Decrypt<T>(string encryptedState);
}
