using API.Models;

namespace API.Services;
public interface ICryptographyService
{
    public string EncryptState(AuthState state);
    public string decryptState(string encryptedState);
}

