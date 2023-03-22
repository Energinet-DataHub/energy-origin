namespace API.Values;

// NOTE: Prefer claim names from this list when not presented in `JwtRegisteredClaimNames`: https://www.iana.org/assignments/jwt/jwt.xhtml
public struct UserClaimName
{
    public const string Scope = "scope";
    public const string AccessToken = "eat";
    public const string IdentityToken = "eit";
    public const string Tin = "tin";
    public const string ProviderId = "ext";
    public const string AcceptedTermsVersion = "atv";
    public const string CurrentTermsVersion = "trm";
    public const string AllowCPRLookup = "acl";
}
