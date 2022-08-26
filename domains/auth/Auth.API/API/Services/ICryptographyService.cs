using API.Models;
using System.Text.Json;

namespace API.Services;
public interface ICryptographyService
{
    string Encrypt(string state);
    string Decrypt(string encryptedState);
    string EncryptJwt(string actor, string subject);
    IdTokenInfo DecodeJwtIdToken(JsonElement jwtToken);
    JwtToken DecodeJwtCustom(string jwtToken);
    bool ValidateJwtToken(string token);
}

