using API.Models;
using API.Repository;
using FluentValidation;

namespace API.TokenStorage;

public class TokenStorage : ITokenStorage
{
    private readonly IValidator<InternalToken> validator;

    public TokenStorage(IValidator<InternalToken> validator)
    {
        this.validator = validator;
    }

    public void DeleteByOpaqueToken(string token)
    {
        throw new NotImplementedException();
    }

    public string GetIdTokenByOpaqueToken(string token)
    {
        throw new NotImplementedException();
    }

    public InternalToken? GetInteralTokenByOpaqueToken(string token)
    {
        // TODO Get interalToken from DB

        var internalToken = new InternalToken();

        return validator.Validate(internalToken).IsValid ? internalToken : null;
    }
}
