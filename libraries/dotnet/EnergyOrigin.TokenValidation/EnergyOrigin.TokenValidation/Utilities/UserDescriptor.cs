using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;

namespace EnergyOrigin.TokenValidation.Utilities;

public class UserDescriptor
{
    public Guid Id { get; init; }
    public ProviderType ProviderType { get; init; }
    public string Name { get; init; } = null!;
    public Guid? CompanyId { get; init; }
    public string? CompanyName { get; init; }
    public List<Role> Roles { get; set; }
    public string? Tin { get; init; }
    public string AcceptedTermsOfServiceVersion { get; init; }
    public string AcceptedPrivacyPolicyVersion { get; init; }
    public string CurrentTermsOfServiceVersion { get; init; }
    public string CurrentPrivacyPolicyVersion { get; init; }
    public bool AllowCPRLookup { get; init; }
    public string EncryptedAccessToken { get; init; } = null!;
    public string EncryptedIdentityToken { get; init; } = null!;
    public bool UserStored { get; init; }

    /// <summary>
    /// The unencrypted data should follow this format: "ProviderKeyType1:ProviderKey1 ProviderKeyType2=ProviderKey2"
    /// </summary>
    public string EncryptedProviderKeys { get; init; } = null!;

    public string? AccessToken => cryptography.Decrypt<string>(EncryptedAccessToken);
    public string? IdentityToken => cryptography.Decrypt<string>(EncryptedIdentityToken);
    public Dictionary<ProviderKeyType, string> ProviderKeys => cryptography.Decrypt<string>(EncryptedProviderKeys)
        .Split(" ")
        .Select(x =>
        {
            var keyValue = x.Split("=");
            return new KeyValuePair<ProviderKeyType, string>(Enum.Parse<ProviderKeyType>(keyValue.First()), keyValue.Last());
        })
        .ToDictionary(x => x.Key, x => x.Value);

    public Guid Subject => CompanyId ?? Id;

    private readonly ICryptography cryptography;

    public UserDescriptor(ICryptography cryptography) => this.cryptography = cryptography;
};
