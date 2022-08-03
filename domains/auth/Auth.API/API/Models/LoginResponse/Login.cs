namespace API.Models;

public class Login
{
    public string FeUrl { get; }
    public string ReturnUrl { get; }

    public Login(string feUrl, string returnUrl)
    {
        FeUrl = feUrl;
        ReturnUrl = returnUrl;
    }
}
