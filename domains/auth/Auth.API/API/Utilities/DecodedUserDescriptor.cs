
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using System.Diagnostics.CodeAnalysis;

namespace API.Utilities;

public class DecodedUserDescriptor
{
    public required string AccessToken { get; init; }
    public required string IdentityToken { get; init; }
    public required Dictionary<ProviderKeyType, string> ProviderKeys { get; init; }

    [SetsRequiredMembers]
    public DecodedUserDescriptor(UserDescriptor user, ICryptography cryptography)
    {
        AccessToken = cryptography.Decrypt<string>(user.EncryptedAccessToken);
        IdentityToken = cryptography.Decrypt<string>(user.EncryptedIdentityToken);
        ProviderKeys = cryptography.Decrypt<string>(user.EncryptedProviderKeys)
            .Split(" ")
            .Select(x =>
            {
                var keyValue = x.Split("=");
                return new KeyValuePair<ProviderKeyType, string>(Enum.Parse<ProviderKeyType>(keyValue.First()), keyValue.Last());
            })
            .ToDictionary(x => x.Key, x => x.Value);
    }
}
