using API.Models;

namespace API.Services;
public interface ICryptographyService
{
    string Encrypt(string state);
    string Decrypt(string encryptedState);
    string EncryptJwt(string actor, string subject);
    IdTokenInfo DecodeJwtIdToken(string jwtToken);
    JwtToken DecodeJwtCustom(string jwtToken);
    bool ValidateJwtToken(string token);
}

