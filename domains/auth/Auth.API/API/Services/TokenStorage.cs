using API.Models;

namespace API.TokenStorage
{
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

        public bool InternalTokenValidation(InternalToken interalToken)
        {
            if (interalToken == null)
            {
                return false;
            }

            if (interalToken.Issued > DateTime.UtcNow || interalToken.Expires < DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }

        public InternalToken? GetInteralTokenByOpaqueToken(string token)
        {
            //throw new NotImplementedException();

            InternalToken interalToken = new InternalToken();
            if (interalToken == null)
            {
                return null;
            }

            var isValid = InternalTokenValidation(interalToken);

            if (isValid != true)
            {
                return null;
            }

            return interalToken;
        }
    }
}
