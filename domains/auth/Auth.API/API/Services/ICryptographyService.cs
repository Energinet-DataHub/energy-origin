namespace API.Services;
public interface ICryptographyService
{
    string Encrypt(string state);
    string Decrypt(string encryptedState);
    string EncryptJwt(string actor, string subject);
    T DecodeJwt<T>(string jwtToken);
}

