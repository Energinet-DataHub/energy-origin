using API.Models;

namespace API.TokenStorage
{
    public interface ITokenStorage
    {
        void DeleteByOpaqueToken(string token);
        string GetIdTokenByOpaqueToken(string token);
        public bool InternalTokenValidation(InternalToken interalToken);
        InternalToken? GetInteralTokenByOpaqueToken(string token);
    }
}
