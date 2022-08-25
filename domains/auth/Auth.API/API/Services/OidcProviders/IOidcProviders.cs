using API.Models;

namespace API.Services.OidcProviders;

public interface IOidcProviders
{
    NextStep CreateAuthorizationUri(AuthState state);
    Task Logout(string token);
}
