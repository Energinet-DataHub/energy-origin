using API.Models;
using API.Repository;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;

namespace API.TokenStorage;

public class TokenStorage : ITokenStorage
{
    private readonly IValidator<InternalToken> validator;
    private readonly IMemoryCache memoryCache;

    public TokenStorage(IValidator<InternalToken> validator, IMemoryCache memoryCache)
    {
        this.validator = validator;
        this.memoryCache = memoryCache;
    }

    public async Task<InternalToken> GetOrCreateInternalToken(InternalToken internalToken) =>
        await memoryCache.GetOrCreateAsync(
            internalToken.OpaqueToken,
            cacheEntry =>
            {
                cacheEntry.SlidingExpiration = TimeSpan.FromMinutes(15);
                return Task.FromResult(internalToken);
            });


    public void DeleteByOpaqueToken(string token) => throw new NotImplementedException();

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
