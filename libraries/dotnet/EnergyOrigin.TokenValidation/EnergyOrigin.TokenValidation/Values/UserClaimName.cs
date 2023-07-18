namespace EnergyOrigin.TokenValidation.Values;

// NOTE: Prefer claim names from this list when not presented in `JwtRegisteredClaimNames`: https://www.iana.org/assignments/jwt/jwt.xhtml
public struct UserClaimName
{
    public const string Scope = "scope";
    public const string Subject = "subject";
    public const string ActorLegacy = "actor";
    public const string Actor = "atr";
    public const string AccessToken = "eat";
    public const string IdentityToken = "eit";
    public const string ProviderKeys = "pke";
    public const string ProviderType = "pty";
    public const string CurrentPrivacyPolicyVersion = "privtrm";
    public const string CurrentTermsOfServiceVersion = "tostrm";
    public const string AcceptedPrivacyPolicyVersion = "privatv";
    public const string AcceptedTermsOfServiceVersion = "tosatv";
    public const string AllowCPRLookup = "acl";
    public const string Tin = "tin";
    public const string CompanyName = "cpn";
    public const string CompanyId = "coi";
    public const string UserStored = "ust";
}
