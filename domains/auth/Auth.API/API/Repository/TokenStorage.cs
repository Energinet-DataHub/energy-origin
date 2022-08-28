using API.Models;
using API.Repository;

namespace API.TokenStorage;

public class TokenStorage : ITokenStorage
{
    public void DeleteByOpaqueToken(string token)
    {
        throw new NotImplementedException();
    }

    public string GetIdTokenByOpaqueToken(string token)
    {
        throw new NotImplementedException();
    }

    public bool InternalTokenValidation(InternalToken internalToken)
    {
        if (internalToken == null)
        {
            return false;
        }

        if (internalToken.Issued > DateTime.UtcNow || internalToken.Expires < DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    public InternalToken? GetInteralTokenByOpaqueToken(string token)
    {
        // TODO Get interalToken from DB

        var internalToken = new InternalToken();
        if (internalToken == null)
        {
            return null;
        }

        var isValid = InternalTokenValidation(internalToken);

        if (!isValid)
        {
            return null;
        }

        return internalToken;
    }
}
