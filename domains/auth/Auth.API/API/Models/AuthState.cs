namespace API.Models;
public record AuthState
{
    public string FeUrl { get; init; }
    public string ReturnUrl { get; init; }
    public bool TermsAccepted { get; init; }
    public string TermsVersion { get; init; }
    public string IdToken { get; init; }
    public string Tin { get; init; }
    public string IdentityProvider { get; init; }
    public string ExternalSubject { get; init; }
}
