using API.Models;

namespace API.Repository;

public interface ITokenStorage
{
    void DeleteByOpaqueToken(string token);
    string GetIdTokenByOpaqueToken(string token);
    public bool InternalTokenValidation(InternalToken internalToken);
    InternalToken? GetInteralTokenByOpaqueToken(string token);
}
