
using EnergyOrigin.TokenValidation.Utilities;
using EnergyOrigin.TokenValidation.Utilities.Interfaces;
using EnergyOrigin.TokenValidation.Values;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace API.Utilities;

public class DecodableUserDescriptor : UserDescriptor
{
    private readonly ICryptography cryptography;

    [SetsRequiredMembers]
    public DecodableUserDescriptor(ClaimsPrincipal? user, ICryptography cryptography) : base(user) => this.cryptography = cryptography;

    public string AccessToken => cryptography.Decrypt<string>(EncryptedAccessToken);
    public string IdentityToken => cryptography.Decrypt<string>(EncryptedIdentityToken);
    public Dictionary<ProviderKeyType, string> ProviderKeys => cryptography.Decrypt<string>(EncryptedProviderKeys)
            .Split(" ")
            .Where(x => x.Contains("="))
            .Select(x =>
            {
                var keyValue = x.Split("=");
                return new KeyValuePair<ProviderKeyType, string>(Enum.Parse<ProviderKeyType>(keyValue.First()), keyValue.Last());
            })
            .ToDictionary(x => x.Key, x => x.Value);
}
