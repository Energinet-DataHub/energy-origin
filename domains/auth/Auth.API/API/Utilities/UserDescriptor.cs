namespace API.Utilities;

public record UserDescriptor(Guid? Id, string ProviderId, string Name, string? Tin, int AcceptedTermsVersion, bool AllowCPRLookup, string EncryptedAccessToken, string EncryptedIdentityToken)
{
    public UserDescriptor(Guid? Id, string ProviderId, string Name, string? Tin, int AcceptedTermsVersion, bool AllowCPRLookup, string EncryptedAccessToken, string EncryptedIdentityToken, ICryptography cryptography) : this(Id, ProviderId, Name, Tin, AcceptedTermsVersion, AllowCPRLookup, EncryptedAccessToken, EncryptedIdentityToken) => this.cryptography = cryptography;
    public string? AccessToken => cryptography.Decrypt<string>(EncryptedAccessToken);
    public string? IdentityToken => cryptography.Decrypt<string>(EncryptedIdentityToken);
    private readonly ICryptography cryptography = null!;
};
