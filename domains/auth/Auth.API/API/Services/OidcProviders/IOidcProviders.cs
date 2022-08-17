using API.Models;

namespace API.Services;
public interface IOidcProviders
{
    NextStep CreateAuthorizationUri(AuthState state);
    Task Logout(string token);
}
