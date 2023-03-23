using System.Linq;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;

namespace EnergyOrigin.TokenValidation.Utilities;

public class UserDescriptor
{
    public Guid? Id { get; init; }
    public ProviderType ProviderType { get; init; }
    public string Name { get; init; } = null!;
    public Guid? CompanyId { get; init; }
    public string? CompanyName { get; init; }
    public string? Tin { get; init; }
    public int AcceptedTermsVersion { get; init; }
    public int CurrentTermsVersion { get; init; }
    public bool AllowCPRLookup { get; init; }
    public string EncryptedAccessToken { get; init; } = null!;
    public string EncryptedIdentityToken { get; init; } = null!;
    public string EncryptedProviderKeys { get; init; } = null!;

    public string? AccessToken => cryptography.Decrypt<string>(EncryptedAccessToken);
    public string? IdentityToken => cryptography.Decrypt<string>(EncryptedIdentityToken);
    // "ProviderKeyType1:ProviderKey1 ProviderKeyType2:ProviderKey2"
    public Dictionary<ProviderKeyType, string> ProviderKeys => cryptography.Decrypt<string>(EncryptedProviderKeys)
        .Split(" ")
        .Select(x =>
        {
            var keyValue = x.Split(":");
            return new KeyValuePair<ProviderKeyType, string>(Enum.Parse<ProviderKeyType>(keyValue.First()), keyValue.Last());
        })
        .ToDictionary(x => x.Key, x => x.Value);

    private readonly ICryptography cryptography;

    public UserDescriptor(ICryptography cryptography) => this.cryptography = cryptography;
};
