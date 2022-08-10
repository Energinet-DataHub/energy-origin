using API.Models;

namespace API.Services;
public interface IOidcProviders
{
    LoginResponse CreateRedirecthUrl(string feUrl, string returnUrl);
}
