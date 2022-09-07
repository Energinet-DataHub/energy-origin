using API.Models;

namespace API.Repository;

public interface ITokenStorage
{
    void DeleteByOpaqueToken(string token);
    string GetIdTokenByOpaqueToken(string token);
    InternalToken? GetInteralTokenByOpaqueToken(string token);
}
