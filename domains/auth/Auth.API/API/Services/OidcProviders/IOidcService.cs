using API.Models;

namespace API.Services.OidcProviders;

public interface IOidcService
{
    NextStep CreateAuthorizationUri(AuthState state);
    Task Logout(string token);
}
