namespace API.Models.Oidc;
public class AuthState
{
    public string FeUrl { get; }
    public string ReturnUrl { get; }

    public AuthState(string feUrl, string returnUrl)
    {
        FeUrl = feUrl;
        ReturnUrl = returnUrl;
    }
}
