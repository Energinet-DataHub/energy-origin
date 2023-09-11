namespace EnergyOrigin.TokenValidation.Values;

// NOTE: Prefer claim names from this list when not presented in `JwtRegisteredClaimNames`: https://www.iana.org/assignments/jwt/jwt.xhtml
public struct UserClaimName
{
    public const string Scope = "scope";
    public const string Subject = "subject";
    public const string Actor = "atr";
    public const string ActorLegacy = "actor";
    public const string AccessToken = "eat";
    public const string IdentityToken = "eit";
    public const string ProviderKeys = "pke";
    public const string ProviderType = "pty";
    public const string AllowCprLookup = "acl";
    public const string Tin = "tin";
    public const string OrganizationName = "cpn";
    public const string OrganizationId = "coi";
    public const string Roles = "roles";
    public const string AssignedRoles = "ars";
    public const string MatchedRoles = "mrs";
}
