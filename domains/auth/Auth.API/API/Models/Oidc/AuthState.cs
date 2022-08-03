namespace API.Models;
public class AuthState
{
    public string FeUrl { get; }
    public string ReturnUrl { get; }
    public bool TermsAccepted { get; }
    public string TermsVersion { get; }
    public string IdToken { get; }
    public string Tin { get; }
    public string IdentityProvider { get; }
    public string ExternalSubject { get; }


    public AuthState(string feUrl, string returnUrl, bool termsAccepted = false, string termsVersion = "0", string idToken = "", string tin = "", string identityProvider = "", string externalSubject = "")
    {
        FeUrl = feUrl;
        ReturnUrl = returnUrl;
        TermsAccepted = termsAccepted;
        TermsVersion = termsVersion;
        IdToken = idToken;
        Tin = tin;
        IdentityProvider = identityProvider;
        ExternalSubject = externalSubject;
    }
}
