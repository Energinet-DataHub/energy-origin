namespace AuthLibrary.Values;

public struct UserScopeClaim
{
    public const string AcceptedTerms = "accepted-terms";
    public const string NotAcceptedTerms = "not-accepted-terms";
    public const string Dashboard = "dashboard";
    public const string Production = "production";
    public const string Meters = "meters";
    public const string Certificates = "certificates";

    public const string AllAcceptedScopes = $"{AcceptedTerms} {Dashboard} {Production} {Meters} {Certificates}";
}
