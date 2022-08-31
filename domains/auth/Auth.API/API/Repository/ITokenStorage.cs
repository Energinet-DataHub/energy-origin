using API.Models;

namespace API.Repository;

public interface ITokenStorage
{
    Task<InternalToken> GetOrCreateInternalToken(InternalToken internalToken);
    void DeleteByOpaqueToken(string token);
    string GetIdTokenByOpaqueToken(string token);
    InternalToken? GetInteralTokenByOpaqueToken(string token);
}
