namespace API.Utilities;

public class ClaimsWrapper
{
    public Guid? Id { get; init; }
    public string ProviderId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string CompanyName { get; init; } = null!;
    public string Tin { get; init; } = null!;
    public int AcceptedTermsVersion { get; init; }
    public bool AllowCPRLookup { get; init; }
    public string EncryptedAccessToken { get; init; } = null!;
    public string EncryptedIdentityToken { get; init; } = null!;

    public string? AccessToken => cryptography.Decrypt<string>(EncryptedAccessToken);
    public string? IdentityToken => cryptography.Decrypt<string>(EncryptedIdentityToken);

    private readonly ICryptography cryptography;

    public ClaimsWrapper(ICryptography cryptography) => this.cryptography = cryptography;
};
