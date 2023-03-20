namespace API.Options;

public class IdentityProviderOptions
{
    public const string Prefix = "IdentityProvider";

    public enum IdentityProvider
    {
        MitIdPrivate,
        MitIdProfessional,
        NemIdPrivate,
        NemIdProfessional
    }

    public List<IdentityProvider> Providers { get; init; } = null!;

    public static IdentityProvider GetIdentityProviderEnum(string providerName, string identityType) => (providerName, identityType) switch
    {
        ("mitid", "private") => IdentityProvider.MitIdPrivate,
        ("mitid", "professional") => IdentityProvider.MitIdProfessional,
        ("nemid", "private") => IdentityProvider.NemIdPrivate,
        ("nemid", "professional") => IdentityProvider.NemIdProfessional,
        _ => throw new NotImplementedException()
    };
}
