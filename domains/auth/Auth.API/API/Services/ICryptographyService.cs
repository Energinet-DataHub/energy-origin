namespace API.Services;
public interface ICryptographyService
{
    string Encrypt(string state);
    string Decrypt(string encryptedState);
    T DecodeJwt<T>(string jwtToken);
}

