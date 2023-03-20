using AuthLibrary.Values;

namespace API.Values;

public struct AcceptedScopes
{
    public const string AllAcceptedScopes = $"{UserScopeClaim.AcceptedTerms} {UserScopeClaim.Dashboard} {UserScopeClaim.Production} {UserScopeClaim.Meters} {UserScopeClaim.Certificates}";       
}
