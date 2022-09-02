using API.Configuration;
using Microsoft.Extensions.Options;

namespace API.Utilities;

public class CryptographyFactory : ICryptographyFactory
{
    private readonly AuthOptions authOptions;

    public CryptographyFactory(IOptions<AuthOptions> authOptions) => this.authOptions = authOptions.Value;

    public ICryptography IdTokenCryptography() => new Cryptography(authOptions.SecretKey);

    public ICryptography StateCryptography() => new Cryptography(authOptions.SecretKey);
}
