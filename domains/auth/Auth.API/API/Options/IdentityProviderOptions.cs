using EnergyOrigin.TokenValidation.Values;

namespace API.Options;

public class IdentityProviderOptions
{
    public const string Prefix = "IdentityProvider";

    public List<ProviderType> Providers { get; init; } = null!;
}
